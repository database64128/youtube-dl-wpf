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
    private readonly Settings _settings;
    private readonly Queue<LogEvent> _queuedLogEvents;
    private readonly IFormatProvider? _formatProvider;

    [Reactive]
    public string Content { get; set; } = "";

    public QueuedTextBoxSink(Settings settings, IFormatProvider? formatProvider = null)
    {
        _settings = settings;
        _queuedLogEvents = new(settings.LoggingMaxEntries);
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_locker)
        {
            if (_queuedLogEvents.Count >= _settings.LoggingMaxEntries)
            {
                _queuedLogEvents.Dequeue();
            }

            _queuedLogEvents.Enqueue(logEvent);

            var messages = _queuedLogEvents.Select(x => x.RenderMessage(_formatProvider));
            var length = messages.Sum(x => x.Length + Environment.NewLine.Length);
            var text = string.Create(length, messages, (buf, msgs) =>
            {
                foreach (var msg in msgs)
                {
                    msg.CopyTo(buf);
                    Environment.NewLine.CopyTo(buf[msg.Length..]);
                    buf = buf[(msg.Length + Environment.NewLine.Length)..];
                }
            });

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Content = text;
            });
        }
    }
}
