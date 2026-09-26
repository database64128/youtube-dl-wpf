using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;
using System;

namespace YoutubeDl.Wpf.ViewModels;

public partial class HistoryItemViewModel(string text, Action<HistoryItemViewModel>? action) : ReactiveObject
{
    [Reactive]
    private string _text = text;

    public bool IsDeleteButtonVisible => action is not null;

    public ReactiveCommand<HistoryItemViewModel, RxVoid> DeleteItemCommand { get; } = action is not null ? ReactiveCommand.Create(action) : s_noOpCommand;

    private static readonly ReactiveCommand<HistoryItemViewModel, RxVoid> s_noOpCommand = ReactiveCommand.Create<HistoryItemViewModel>(_ => { });

    public override string ToString() => _text;
}
