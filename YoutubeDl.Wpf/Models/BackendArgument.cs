using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace YoutubeDl.Wpf.Models;

/// <summary>
/// BackendArgument wraps an argument string into a POCO
/// so it can be easily removed from a collection.
/// </summary>
public partial class BackendArgument : ReactiveObject
{
    [Reactive]
    public partial string Argument { get; set; }

    public BackendArgument() => Argument = "";

    public BackendArgument(string argument) => Argument = argument;

    public override string ToString() => Argument;
}
