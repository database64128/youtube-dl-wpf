using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;

namespace YoutubeDl.Wpf.Models;

public class QueuedTextBoxSink : ReactiveObject, ILogEventSink
{
    private readonly object _locker = new();
    private readonly ObservableSettings _settings;
    private readonly Queue<LogEvent> _queuedLogEvents;
    private readonly IFormatProvider? _formatProvider;

    [Reactive]
    public string Content { get; set; } = "";

    public QueuedTextBoxSink(ObservableSettings settings, IFormatProvider? formatProvider = null)
    {
        _settings = settings;
        _queuedLogEvents = new(settings.LoggingMaxEntries);
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        // Workaround for https://github.com/reactiveui/ReactiveUI/issues/3415 before upstream has a fix.
        if (logEvent.MessageTemplate.Text.EndsWith(" is a POCO type and won't send change notifications, WhenAny will only return a single value!"))
        {
            return;
        }

        lock (_locker)
        {
            if (_queuedLogEvents.Count >= _settings.LoggingMaxEntries)
            {
                _queuedLogEvents.Dequeue();
            }

            _queuedLogEvents.Enqueue(logEvent);

            var messages = _queuedLogEvents.Select(x => (x.Timestamp.ToString("O"), x.Level, x.RenderMessage(_formatProvider)));
            var length = messages.Sum(x => x.Item1.Length + 1 + 3 + 1 + x.Item3.Length + Environment.NewLine.Length);
            var text = string.Create(length, messages, (buf, msgs) =>
            {
                foreach (var msg in msgs)
                {
                    msg.Item1.CopyTo(buf);
                    buf[msg.Item1.Length] = ' ';
                    buf = buf[(msg.Item1.Length + 1)..];

                    buf[0] = '[';
                    buf[1] = msg.Level.ToString()[0];
                    buf[2] = ']';
                    buf[3] = ' ';
                    buf = buf[4..];

                    msg.Item3.CopyTo(buf);
                    Environment.NewLine.CopyTo(buf[msg.Item3.Length..]);
                    buf = buf[(msg.Item3.Length + Environment.NewLine.Length)..];
                }
            });

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Content = text;
            });
        }
    }
}
