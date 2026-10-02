using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.Tests;

public class PresetToArgsTests
{
    [Test]
    [Arguments("", "", "", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { })]
    [Arguments("testName", "", "", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { })]
    [Arguments("testName", "248+251", "", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", })]
    [Arguments("testName", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [Arguments("testName", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { "-v" }, new string[] { "-f", "248+251", "--merge-output-format", "webm", "-v", })]
    [Arguments("", "248+251", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [Arguments("testName", "", "webm", BackendTypes.Ytdl | BackendTypes.Ytdlp, new string[] { }, new string[] { "--merge-output-format", "webm", })]
    [Arguments("testName", "248+251", "webm", BackendTypes.Ytdl, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    [Arguments("testName", "248+251", "webm", BackendTypes.Ytdlp, new string[] { }, new string[] { "-f", "248+251", "--merge-output-format", "webm", })]
    public async Task Preset_ToArgs(
        string name,
        string formatArg,
        string containerArg,
        BackendTypes supportedBackends,
        string[] extraArgs,
        string[] expectedArgs)
    {
        var preset = new Preset(name, formatArg, containerArg, supportedBackends, extraArgs);
        var args = preset.ToArgs();

        await Assert.That(args).IsEquivalentTo(expectedArgs);
    }
}
