using Xunit;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.Tests;

public class PresetToArgsTests
{
    [Theory]
    [InlineData("", null, null, BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { })]
    [InlineData("testName", null, null, BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { })]
    [InlineData("testName", "248+251", null, BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { "-v" }, new string[] { "-f", "248+251", "--merge-output-format", "webm", "-v", })]
    [InlineData("", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [InlineData("testName", null, "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "--merge-output-format", "webm", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdl, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    public void Preset_ToArgs(
        string name,
        string? formatArg,
        string? containerArg,
        BackendTypes supportedBackends,
        string[] extraArgs,
        string[] expectedArgs)
    {
        var preset = new Preset(name, formatArg, containerArg, supportedBackends, extraArgs);
        var args = preset.ToArgs();

        Assert.Equal(expectedArgs, args);
    }
}
