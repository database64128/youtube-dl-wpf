using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public partial class ArgumentChipViewModel(BackendArgument argument, bool isRemovable, Action<ArgumentChipViewModel> action) : ReactiveValidationObject
{
    [Reactive]
    private BackendArgument _argument = argument;

    [Reactive]
    private bool _isRemovable = isRemovable;

    public ReactiveCommand<ArgumentChipViewModel, Unit> RemoveArgumentCommand { get; } = ReactiveCommand.Create(action);
}
