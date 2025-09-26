using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading;

namespace YoutubeDl.Wpf.Models;

public partial class QueuedTextBoxSink(Settings settings, IFormatProvider? formatProvider = null) : ReactiveObject, ILogEventSink
{
    private readonly Lock _lock = new();
    private readonly Queue<string> _queuedLogMessages = new(settings.LoggingMaxEntries);
    private int _contentLength;

    [Reactive]
    private string _content = "";

    public void Emit(LogEvent logEvent)
    {
        // Workaround for https://github.com/reactiveui/ReactiveUI/issues/3415 before upstream has a fix.
        if (logEvent.MessageTemplate.Text.EndsWith(" is a POCO type and won't send change notifications, WhenAny will only return a single value!", StringComparison.Ordinal))
        {
            return;
        }

        string renderedMessage = logEvent.RenderMessage(formatProvider);
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
            while (_queuedLogMessages.Count >= settings.LoggingMaxEntries)
            {
                string dequeuedMessage = _queuedLogMessages.Dequeue();
                _contentLength -= dequeuedMessage.Length;
            }

            _queuedLogMessages.Enqueue(message);
            _contentLength += message.Length;

            string content = string.Create(_contentLength, _queuedLogMessages, (buf, msgs) =>
            {
                foreach (string msg in msgs)
                {
                    msg.CopyTo(buf);
                    buf = buf[msg.Length..];
                }
            });

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Content = content;
            });
        }
    }
}
