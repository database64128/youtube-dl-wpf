using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Reactive;

namespace YoutubeDl.Wpf.ViewModels;

public partial class HistoryItemViewModel(string text, Action<HistoryItemViewModel>? action) : ReactiveObject
{
    [Reactive]
    private string _text = text;

    public bool IsDeleteButtonVisible => action is not null;

    public ReactiveCommand<HistoryItemViewModel, Unit> DeleteItemCommand { get; } = action is not null ? ReactiveCommand.Create(action) : s_noOpCommand;

    private static readonly ReactiveCommand<HistoryItemViewModel, Unit> s_noOpCommand = ReactiveCommand.Create<HistoryItemViewModel>(_ => { });

    public override string ToString() => _text;
}
