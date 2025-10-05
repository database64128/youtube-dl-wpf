using System.Collections.Generic;

namespace YoutubeDl.Wpf.Models;

public record Preset(
    string Name,
    string FormatArg = "",
    string ContainerArg = "",
    BackendTypes SupportedBackends = BackendTypes.Ytdl | BackendTypes.Ytdlp,
    params string[] ExtraArgs)
{
    public bool IsPredefined { get; private init; }

    private static Preset CreatePredefined(
        string name,
        string formatArg = "",
        string containerArg = "",
        BackendTypes supportedBackends = BackendTypes.Ytdl | BackendTypes.Ytdlp,
        params string[] extraArgs) =>
        new(name, formatArg, containerArg, supportedBackends, extraArgs) { IsPredefined = true, };

    public Preset Duplicate(string newName) => this with { Name = newName, IsPredefined = false, };

    public IEnumerable<string> GetNonExtraArgs()
    {
        if (!string.IsNullOrEmpty(FormatArg))
        {
            yield return "-f";
            yield return FormatArg;
        }

        if (!string.IsNullOrEmpty(ContainerArg))
        {
            yield return "--merge-output-format";
            yield return ContainerArg;
        }
    }

    public IEnumerable<string> ToArgs()
    {
        foreach (var arg in GetNonExtraArgs())
        {
            yield return arg;
        }

        foreach (var extraArg in ExtraArgs)
        {
            yield return extraArg;
        }
    }

    public const string AutoName = "Auto";

    public static readonly Preset Auto = CreatePredefined(AutoName);

    public static readonly Preset Empty = new("");

    public static readonly Preset[] PredefinedPresets =
    [
        Auto,
        CreatePredefined("bestvideo+bestaudio/best", formatArg: "bestvideo+bestaudio/best"),
        CreatePredefined("bestvideo+bestaudio", formatArg: "bestvideo+bestaudio"),
        CreatePredefined("bestvideo+worstaudio", formatArg: "bestvideo+worstaudio"),
        CreatePredefined("worstvideo+bestaudio", formatArg: "worstvideo+bestaudio"),
        CreatePredefined("worstvideo+worstaudio", formatArg : "worstvideo+worstaudio"),
        CreatePredefined("worstvideo+worstaudio/worst", formatArg : "worstvideo+worstaudio/worst"),
        CreatePredefined("best", formatArg : "best"),
        CreatePredefined("worst", formatArg : "worst"),
        CreatePredefined("bestvideo", formatArg : "bestvideo"),
        CreatePredefined("worstvideo", formatArg : "worstvideo"),
        CreatePredefined("bestaudio", formatArg : "bestaudio"),
        CreatePredefined("worstaudio", formatArg : "worstaudio"),
        CreatePredefined("YouTube 4K 60fps HDR AV1 + Opus WebM (701+251)", "701+251", "webm"),
        CreatePredefined("YouTube 4K 60fps HDR VP9 + Opus WebM (337+251)", "337+251", "webm"),
        CreatePredefined("YouTube 4K 60fps AV1 + Opus WebM (401+251)", "401+251", "webm"),
        CreatePredefined("YouTube 4K 60fps VP9 + Opus WebM (315+251)", "315+251", "webm"),
        CreatePredefined("YouTube 4K AV1 + Opus WebM (401+251)", "401+251", "webm"),
        CreatePredefined("YouTube 4K VP9 + Opus WebM (313+251)", "313+251", "webm"),
        CreatePredefined("YouTube 1440p60 HDR AV1 + Opus WebM (700+251)", "700+251", "webm"),
        CreatePredefined("YouTube 1440p60 HDR VP9 + Opus WebM (336+251)", "336+251", "webm"),
        CreatePredefined("YouTube 1440p60 AV1 + Opus WebM (400+251)", "400+251", "webm"),
        CreatePredefined("YouTube 1440p60 VP9 + Opus WebM (308+251)", "308+251", "webm"),
        CreatePredefined("YouTube 1440p AV1 + Opus WebM (400+251)", "400+251", "webm"),
        CreatePredefined("YouTube 1440p VP9 + Opus WebM (271+251)", "271+251", "webm"),
        CreatePredefined("YouTube 1080p60 AV1 + Opus WebM (399+251)", "399+251", "webm"),
        CreatePredefined("YouTube 1080p60 VP9 + Opus WebM (303+251)", "303+251", "webm"),
        CreatePredefined("YouTube 1080p60 AVC + AAC MP4 (299+140)", "299+140"),
        CreatePredefined("YouTube 1080p AV1 + Opus WebM (399+251)", "399+251", "webm"),
        CreatePredefined("YouTube 1080p VP9 + Opus WebM (248+251)", "248+251", "webm"),
        CreatePredefined("YouTube 1080p AVC + AAC MP4 (137+140)", "137+140"),
        CreatePredefined("YouTube 720p60 AV1 + Opus WebM (398+251)", "398+251", "webm"),
        CreatePredefined("YouTube 720p60 VP9 + Opus WebM (302+251)", "302+251", "webm"),
        CreatePredefined("YouTube 720p60 AVC + AAC MP4 (298+140)", "298+140"),
        CreatePredefined("YouTube 720p AV1 + Opus WebM (398+251)", "398+251", "webm"),
        CreatePredefined("YouTube 720p VP9 + Opus WebM (247+251)", "247+251", "webm"),
        CreatePredefined("YouTube 720p AVC + AAC (136+140)", "136+140"),
        CreatePredefined("YouTube Opus Audio (251)", "251", "", BackendTypes.Ytdlp, "--remux-video", "opus"),
        CreatePredefined("YouTube AAC Audio (140)", "140"),
        CreatePredefined("YouTube Music High 256kbps AAC (141)", "141"),
        CreatePredefined("YouTube Music High 256kbps Opus (774)", "774"),
        CreatePredefined("1080p", formatArg : "1080p"),
        CreatePredefined("720p", formatArg : "720p"),
        CreatePredefined("webm", formatArg: "webm"),
        CreatePredefined("mp4", formatArg : "mp4"),
        CreatePredefined("mkv", formatArg : "mkv"),
        CreatePredefined("opus", formatArg : "opus"),
        CreatePredefined("flac", formatArg : "flac"),
        CreatePredefined("ogg", formatArg : "ogg"),
        CreatePredefined("m4a", formatArg: "m4a"),
        CreatePredefined("mp3", formatArg : "mp3"),
    ];
}
