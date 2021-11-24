namespace YoutubeDl.Wpf.Models;

/// <summary>
/// BackendArgument wraps an argument string into a POCO
/// so it can be easily removed from a collection.
/// </summary>
public class BackendArgument
{
    public string Argument { get; set; }

    public BackendArgument() => Argument = "";

    public BackendArgument(string argument) => Argument = argument;

    public override string ToString() => Argument;
}
