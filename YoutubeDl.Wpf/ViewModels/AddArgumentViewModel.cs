using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;

namespace YoutubeDl.Wpf.ViewModels;

public class AddArgumentViewModel : ReactiveValidationObject
{
    [Reactive]
    public string Argument { get; set; } = "";

    public ReactiveCommand<string, Unit> AddArgumentCommand { get; }

    public AddArgumentViewModel(Action<string> action)
    {
        var canAddArgument = this.WhenAnyValue(x => x.Argument, arg => !string.IsNullOrEmpty(arg));

        AddArgumentCommand = ReactiveCommand.Create((string x) =>
        {
            action(x);
            Argument = "";
        }, canAddArgument);
    }
}
