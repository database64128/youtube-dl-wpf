using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace YoutubeDl.Wpf.Models;

public class BackendService(ObservableSettings settings) : ReactiveObject, IEnableLogger
{
    public List<BackendInstance> Instances { get; } = [];

    [Reactive]
    public bool CanUpdate { get; set; } = true;

    [Reactive]
    public double GlobalDownloadProgressPercentage { get; set; } // 0.99 is 99%.

    [Reactive]
    public TaskbarItemProgressState ProgressState { get; set; }

    public BackendInstance CreateInstance()
    {
        var instance = new BackendInstance(settings, this);
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

    public Task UpdateBackendAsync(CancellationToken cancellationToken = default)
    {
        var tasks = Instances.Select(x => x.UpdateAsync(cancellationToken));
        return Task.WhenAll(tasks);
    }
}
