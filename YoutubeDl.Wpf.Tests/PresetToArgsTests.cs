using Xunit;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.Tests;

public class PresetToArgsTests
{
    [Theory]
    [InlineData(null, null, null, BackendTypes.Ytdl | BackendTypes.Ytdlp, true, new string[] { }, new string[] { })]
    [InlineData("testName", null, null, BackendTypes.Ytdl | BackendTypes.Ytdlp, true, new string[] { }, new string[] { })]
    [InlineData("testName", "248+251", null, BackendTypes.Ytdl | BackendTypes.Ytdlp, true, new string[] { }, new string[] { "-f", "248+251", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, true, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, true, new string[] { "-v" }, new string[] { "-f", "248+251", "--merge-output-format", "webm", "-v", })]
    [InlineData(null, "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, true, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [InlineData("testName", null, "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, true, new string[] { }, new string[] { "-f", "webm", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdl, true, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdlp, true, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [InlineData("testName", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, false, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    public void Preset_ToArgs(
        string? name,
        string? formatArg,
        string? containerArg,
        BackendTypes supportedBackends,
        bool isPredefined,
        string[] extraArgs,
        string[] expectedArgs)
    {
        var preset = new Preset(name, formatArg, containerArg, supportedBackends, isPredefined, extraArgs);
        var args = preset.ToArgs();

        Assert.Equal(expectedArgs, args);
    }
}
