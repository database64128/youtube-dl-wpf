using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace YoutubeDl.Wpf.Models;

public partial class QueuedTextBoxSink : ReactiveObject, ILogEventSink
{
    private readonly Settings _settings;
    private readonly IFormatProvider? _formatProvider;

    private readonly Lock _lock = new();
    private readonly Queue<string> _queuedLogMessages;
    private int _contentLength;

    private readonly struct Signal { }
    private readonly ChannelWriter<Signal> _contentUpdateSignalWriter;

    [Reactive]
    private string _content = "";

    public QueuedTextBoxSink(Settings settings, IFormatProvider? formatProvider = null)
    {
        _settings = settings;
        _formatProvider = formatProvider;
        _queuedLogMessages = new(settings.LoggingMaxEntries);

        Channel<Signal> channel = Channel.CreateBounded<Signal>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropWrite, // This assumes that Signal carries no data.
        });
        _contentUpdateSignalWriter = channel.Writer;
        _ = UpdateContentAsync(channel.Reader);
    }

    public void Emit(LogEvent logEvent)
    {
        // Workaround for https://github.com/reactiveui/ReactiveUI/issues/3415 before upstream has a fix.
        if (logEvent.MessageTemplate.Text.EndsWith(" is a POCO type and won't send change notifications, WhenAny will only return a single value!", StringComparison.Ordinal))
        {
            return;
        }

        string renderedMessage = logEvent.RenderMessage(_formatProvider);
        string exceptionString = logEvent.Exception?.ToString() ?? "";

        // 2023-04-24T10:24:00.000+00:00 [I] Hi!
        int length = 29 + 1 + 3 + 1 + renderedMessage.Length + Environment.NewLine.Length;
        if (exceptionString.Length > 0)
        {
            length += exceptionString.Length + Environment.NewLine.Length;
        }

        string message = string.Create(length, logEvent, (buf, logEvent) =>
        {
            if (!logEvent.Timestamp.TryFormat(buf, out int charsWritten, "yyyy-MM-ddTHH:mm:ss.fffzzz"))
                throw new Exception("Failed to format timestamp for log message.");

            Debug.Assert(charsWritten == 29, $"Expected timestamp to be 29 characters, but got {charsWritten}.");

            buf[29] = ' ';
            buf[30] = '[';
            buf[31] = logEvent.Level switch
            {
                LogEventLevel.Verbose => 'V',
                LogEventLevel.Debug => 'D',
                LogEventLevel.Information => 'I',
                LogEventLevel.Warning => 'W',
                LogEventLevel.Error => 'E',
                LogEventLevel.Fatal => 'F',
                _ => '?',
            };
            buf[32] = ']';
            buf[33] = ' ';
            buf = buf[34..];

            renderedMessage.CopyTo(buf);
            buf = buf[renderedMessage.Length..];

            if (exceptionString.Length > 0)
            {
                Environment.NewLine.CopyTo(buf);
                buf = buf[Environment.NewLine.Length..];

                exceptionString.CopyTo(buf);
                buf = buf[exceptionString.Length..];
            }

            Environment.NewLine.CopyTo(buf);
        });

        lock (_lock)
        {
            while (_queuedLogMessages.Count >= _settings.LoggingMaxEntries)
            {
                string dequeuedMessage = _queuedLogMessages.Dequeue();
                _contentLength -= dequeuedMessage.Length;
            }

            _queuedLogMessages.Enqueue(message);
            _contentLength += message.Length;
        }

        _ = _contentUpdateSignalWriter.TryWrite(default);
    }

    private async Task UpdateContentAsync(ChannelReader<Signal> reader, CancellationToken cancellationToken = default)
    {
        await foreach (Signal _ in reader.ReadAllAsync(cancellationToken))
        {
            string content;

            lock (_lock)
            {
                content = string.Create(_contentLength, _queuedLogMessages, (buf, msgs) =>
                {
                    foreach (string msg in msgs)
                    {
                        msg.CopyTo(buf);
                        buf = buf[msg.Length..];
                    }
                });
            }

            Content = content;

            const int updateIntervalMs = 100;
            await Task.Delay(updateIntervalMs, cancellationToken);
        }
    }

    [ReactiveCommand]
    public void Clear()
    {
        lock (_lock)
        {
            _queuedLogMessages.Clear();
            _contentLength = 0;
        }

        Content = "";
    }
}
