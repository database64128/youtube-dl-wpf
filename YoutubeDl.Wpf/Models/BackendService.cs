using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Shell;

namespace YoutubeDl.Wpf.Models;

public class BackendService : ReactiveObject, IEnableLogger
{
    private readonly Settings _settings;

    public List<BackendInstance> Instances { get; } = new();

    [Reactive]
    public bool CanUpdate { get; set; } = true;

    [Reactive]
    public double GlobalDownloadProgressPercentage { get; set; } // 0.99 is 99%.

    [Reactive]
    public TaskbarItemProgressState ProgressState { get; set; }

    public BackendService(Settings settings) => _settings = settings;

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

    public void UpdateBackend()
    {
        var instance = Instances.Count switch
        {
            > 0 => Instances[0],
            _ => new(_settings, this),
        };

        instance.UpdateDl();
    }
}
