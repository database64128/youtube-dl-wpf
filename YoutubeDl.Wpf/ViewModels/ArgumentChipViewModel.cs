using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public class ArgumentChipViewModel : ReactiveValidationObject
{
    [Reactive]
    public BackendArgument Argument { get; set; }

    [Reactive]
    public bool IsRemovable { get; set; }

    public ReactiveCommand<ArgumentChipViewModel, Unit> RemoveArgumentCommand { get; }

    public ArgumentChipViewModel(BackendArgument argument, bool isRemovable, Action<ArgumentChipViewModel> action)
    {
        Argument = argument;
        IsRemovable = isRemovable;
        RemoveArgumentCommand = ReactiveCommand.Create(action);
    }
}
