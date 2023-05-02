using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;

namespace YoutubeDl.Wpf.ViewModels;

public class HistoryItemViewModel : ReactiveValidationObject
{
    [Reactive]
    public string Text { get; set; }

    public ReactiveCommand<HistoryItemViewModel, Unit> DeleteItemCommand { get; }

    public HistoryItemViewModel(string text, Action<HistoryItemViewModel> action)
    {
        Text = text;
        DeleteItemCommand = ReactiveCommand.Create(action);
    }

    public override string ToString() => Text;
}
