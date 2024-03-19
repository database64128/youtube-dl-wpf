using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace YoutubeDl.Wpf.Models;

public class QueuedTextBoxSink(Settings settings, IFormatProvider? formatProvider = null) : ReactiveObject, ILogEventSink
{
    private readonly object _locker = new();
    private readonly Queue<string> _queuedLogMessages = new(settings.LoggingMaxEntries);
    private int _contentLength;

    [Reactive]
    public string Content { get; set; } = "";

    public void Emit(LogEvent logEvent)
    {
        // Workaround for https://github.com/reactiveui/ReactiveUI/issues/3415 before upstream has a fix.
        if (logEvent.MessageTemplate.Text.EndsWith(" is a POCO type and won't send change notifications, WhenAny will only return a single value!", StringComparison.Ordinal))
        {
            return;
        }

        var renderedMessage = logEvent.RenderMessage(formatProvider);

        // 2023-04-24T10:24:00.000+00:00 [I] Hi!
        var length = 29 + 1 + 3 + 1 + renderedMessage.Length + Environment.NewLine.Length;
        var message = string.Create(length, logEvent, (buf, logEvent) =>
        {
            if (!logEvent.Timestamp.TryFormat(buf, out var charsWritten, "yyyy-MM-ddTHH:mm:ss.fffzzz"))
                throw new Exception("Failed to format timestamp for log message.");
            if (charsWritten != 29)
                throw new Exception($"Unexpected formatted timestamp length {charsWritten}.");

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
            renderedMessage.CopyTo(buf[34..]);
            Environment.NewLine.CopyTo(buf[(34 + renderedMessage.Length)..]);
        });

        lock (_locker)
        {
            while (_queuedLogMessages.Count >= settings.LoggingMaxEntries)
            {
                var dequeuedMessage = _queuedLogMessages.Dequeue();
                _contentLength -= dequeuedMessage.Length;
            }

            _queuedLogMessages.Enqueue(message);
            _contentLength += message.Length;

            var content = string.Create(_contentLength, _queuedLogMessages, (buf, msgs) =>
            {
                foreach (var msg in msgs)
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
