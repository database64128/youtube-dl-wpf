using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;
using System;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public partial class ArgumentChipViewModel(BackendArgument argument, bool isRemovable, Action<ArgumentChipViewModel> action) : ReactiveObject
{
    [Reactive]
    private BackendArgument _argument = argument;

    [Reactive]
    private bool _isRemovable = isRemovable;

    public ReactiveCommand<ArgumentChipViewModel, RxVoid> RemoveArgumentCommand { get; } = ReactiveCommand.Create(action);
}
