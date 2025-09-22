using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;

namespace YoutubeDl.Wpf.ViewModels;

public partial class HistoryItemViewModel(string text, Action<HistoryItemViewModel> action) : ReactiveValidationObject
{
    [Reactive]
    private string _text = text;

    public ReactiveCommand<HistoryItemViewModel, Unit> DeleteItemCommand { get; } = ReactiveCommand.Create(action);

    public override string ToString() => _text;
}
