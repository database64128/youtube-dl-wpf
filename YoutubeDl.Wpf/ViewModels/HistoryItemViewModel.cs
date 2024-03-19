using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;

namespace YoutubeDl.Wpf.ViewModels;

public class HistoryItemViewModel(string text, Action<HistoryItemViewModel> action) : ReactiveValidationObject
{
    [Reactive]
    public string Text { get; set; } = text;

    public ReactiveCommand<HistoryItemViewModel, Unit> DeleteItemCommand { get; } = ReactiveCommand.Create(action);

    public override string ToString() => Text;
}
