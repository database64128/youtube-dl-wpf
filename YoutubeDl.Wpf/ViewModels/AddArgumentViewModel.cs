using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Helpers;
using System;
using System.Reactive;

namespace YoutubeDl.Wpf.ViewModels;

public partial class AddArgumentViewModel : ReactiveValidationObject
{
    [Reactive]
    private string _argument = "";

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
