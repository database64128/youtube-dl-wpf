using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;

namespace YoutubeDl.Wpf.ViewModels;

public class DownloadPathItemViewModel : ReactiveValidationObject
{
    [Reactive]
    public string Path { get; set; }

    public ReactiveCommand<DownloadPathItemViewModel, Unit> DeleteItemCommand { get; }

    public DownloadPathItemViewModel(string path, Action<DownloadPathItemViewModel> action)
    {
        Path = path;
        DeleteItemCommand = ReactiveCommand.Create(action);
    }

    public override string ToString() => Path;
}
