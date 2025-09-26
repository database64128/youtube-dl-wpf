﻿using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace YoutubeDl.Wpf.Models;

public partial class BackendService : ReactiveObject, IEnableLogger
{
    private readonly ObservableSettings _settings;
    private readonly IObservable<bool> _canUpdateBackend;

    public List<BackendInstance> Instances { get; } = [];

    [Reactive]
    private bool _canUpdate = true;

    [Reactive]
    private double _globalDownloadProgressPercentage; // 0.99 is 99%.

    [Reactive]
    private TaskbarItemProgressState _progressState;

    public BackendService(ObservableSettings settings)
    {
        _settings = settings;
        _canUpdateBackend = this.WhenAnyValue(
            x => x.CanUpdate,
            x => x._settings.IsDlBinaryValid,
            (canUpdate, isDlBinaryValid) => canUpdate && isDlBinaryValid);
    }

    public BackendInstance CreateInstance()
    {
        var instance = new BackendInstance(_settings, this);
        Instances.Add(instance);
        return instance;
    }

    public void UpdateProgress()
    {
        CanUpdate = Instances.All(x => !x.IsRunning);

        GlobalDownloadProgressPercentage = Instances.Sum(x => x.DownloadProgressPercentage) / Instances.Count;

        if (Instances.All(x => x.StatusIndeterminate))
        {
            ProgressState = TaskbarItemProgressState.Indeterminate;
        }
        else if (GlobalDownloadProgressPercentage > 0.0)
        {
            ProgressState = TaskbarItemProgressState.Normal;
        }
        else
        {
            ProgressState = TaskbarItemProgressState.None;
        }
    }

    [ReactiveCommand(CanExecute = nameof(_canUpdateBackend))]
    public Task UpdateBackendAsync(CancellationToken cancellationToken = default)
    {
        var tasks = Instances.Select(x => x.UpdateAsync(cancellationToken));
        return Task.WhenAll(tasks);
    }
}
