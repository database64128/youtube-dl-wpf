using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using System.Windows.Shell;

namespace YoutubeDl.Wpf.Models;

public partial class BackendService : ReactiveObject, IEnableLogger
{
    private readonly IObservable<bool> _canUpdateBackend;

    public ObservableSettings SharedSettings { get; }

    public List<BackendInstance> Instances { get; } = [];

    [Reactive]
    private bool _canUpdate = true;

    [Reactive]
    private double _globalDownloadProgressPercentage; // 0.99 is 99%.

    [Reactive]
    private TaskbarItemProgressState _progressState;

    public BackendService(ObservableSettings settings)
    {
        SharedSettings = settings;
        _canUpdateBackend = this.WhenAnyValue(
            x => x.CanUpdate,
            x => x.SharedSettings.IsDlBinaryValid,
            (canUpdate, isDlBinaryValid) => canUpdate && isDlBinaryValid);
    }

    public BackendInstance CreateInstance()
    {
        var instance = new BackendInstance(SharedSettings, this);
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
