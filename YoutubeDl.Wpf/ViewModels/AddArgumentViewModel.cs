using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using ReactiveUI.Primitives;

namespace YoutubeDl.Wpf.ViewModels;

public partial class AddArgumentViewModel : ReactiveObject
{
    [Reactive]
    private string _argument = "";

    public ReactiveCommand<string, RxVoid> AddArgumentCommand { get; }

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
