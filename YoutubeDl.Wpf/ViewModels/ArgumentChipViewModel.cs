using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public class ArgumentChipViewModel(BackendArgument argument, bool isRemovable, Action<ArgumentChipViewModel> action) : ReactiveValidationObject
{
    [Reactive]
    public BackendArgument Argument { get; set; } = argument;

    [Reactive]
    public bool IsRemovable { get; set; } = isRemovable;

    public ReactiveCommand<ArgumentChipViewModel, Unit> RemoveArgumentCommand { get; } = ReactiveCommand.Create(action);
}
