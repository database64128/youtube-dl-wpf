using System.Collections.Generic;

namespace YoutubeDl.Wpf.Models;

public record Preset(
    string? Name = null,
    string? FormatArg = null,
    string? ContainerArg = null,
    BackendTypes SupportedBackends = BackendTypes.Ytdl | BackendTypes.Ytdlp,
    bool IsPredefined = false,
    params string[] ExtraArgs)
{
    public string DisplayName => Name ?? FormatArg ?? ContainerArg ?? "unnamed";

    public IEnumerable<string> GetNonExtraArgs()
    {
        if (this == Auto)
        {
            yield break;
        }

        var option = "-f";

        if (FormatArg is not null)
        {
            yield return option;
            yield return FormatArg;
            option = "--merge-output-format";
        }

        if (ContainerArg is not null)
        {
            yield return option;
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

    public static readonly Preset Auto = new(AutoName, IsPredefined: true);

    public static readonly Preset Empty = new();

    public static readonly Preset[] PredefinedPresets =
    [
        Auto,
        new(FormatArg: "bestvideo+bestaudio/best", IsPredefined: true),
        new(FormatArg: "bestvideo+bestaudio", IsPredefined: true),
        new(FormatArg: "bestvideo+worstaudio", IsPredefined: true),
        new(FormatArg: "worstvideo+bestaudio", IsPredefined: true),
        new(FormatArg: "worstvideo+worstaudio", IsPredefined: true),
        new(FormatArg: "worstvideo+worstaudio/worst", IsPredefined: true),
        new(FormatArg: "best", IsPredefined: true),
        new(FormatArg: "worst", IsPredefined: true),
        new(FormatArg: "bestvideo", IsPredefined: true),
        new(FormatArg: "worstvideo", IsPredefined: true),
        new(FormatArg: "bestaudio", IsPredefined: true),
        new(FormatArg: "worstaudio", IsPredefined: true),
        new("YouTube 4K 60fps HDR AV1 + Opus WebM (701+251)", "701+251", "webm", IsPredefined: true),
        new("YouTube 4K 60fps HDR VP9 + Opus WebM (337+251)", "337+251", "webm", IsPredefined: true),
        new("YouTube 4K 60fps AV1 + Opus WebM (401+251)", "401+251", "webm", IsPredefined: true),
        new("YouTube 4K 60fps VP9 + Opus WebM (315+251)", "315+251", "webm", IsPredefined: true),
        new("YouTube 4K AV1 + Opus WebM (401+251)", "401+251", "webm", IsPredefined: true),
        new("YouTube 4K VP9 + Opus WebM (313+251)", "313+251", "webm", IsPredefined: true),
        new("YouTube 1440p60 HDR AV1 + Opus WebM (700+251)", "700+251", "webm", IsPredefined: true),
        new("YouTube 1440p60 HDR VP9 + Opus WebM (336+251)", "336+251", "webm", IsPredefined: true),
        new("YouTube 1440p60 AV1 + Opus WebM (400+251)", "400+251", "webm", IsPredefined: true),
        new("YouTube 1440p60 VP9 + Opus WebM (308+251)", "308+251", "webm", IsPredefined: true),
        new("YouTube 1440p AV1 + Opus WebM (400+251)", "400+251", "webm", IsPredefined: true),
        new("YouTube 1440p VP9 + Opus WebM (271+251)", "271+251", "webm", IsPredefined: true),
        new("YouTube 1080p60 AV1 + Opus WebM (399+251)", "399+251", "webm", IsPredefined: true),
        new("YouTube 1080p60 VP9 + Opus WebM (303+251)", "303+251", "webm", IsPredefined: true),
        new("YouTube 1080p60 AVC + AAC MP4 (299+140)", "299+140", IsPredefined: true),
        new("YouTube 1080p AV1 + Opus WebM (399+251)", "399+251", "webm", IsPredefined: true),
        new("YouTube 1080p VP9 + Opus WebM (248+251)", "248+251", "webm", IsPredefined: true),
        new("YouTube 1080p AVC + AAC MP4 (137+140)", "137+140", IsPredefined: true),
        new("YouTube 720p60 AV1 + Opus WebM (398+251)", "398+251", "webm", IsPredefined: true),
        new("YouTube 720p60 VP9 + Opus WebM (302+251)", "302+251", "webm", IsPredefined: true),
        new("YouTube 720p60 AVC + AAC MP4 (298+140)", "298+140", IsPredefined: true),
        new("YouTube 720p AV1 + Opus WebM (398+251)", "398+251", "webm", IsPredefined: true),
        new("YouTube 720p VP9 + Opus WebM (247+251)", "247+251", "webm", IsPredefined: true),
        new("YouTube 720p AVC + AAC (136+140)", "136+140", IsPredefined: true),
        new("YouTube Opus Audio (251)", "251", null, BackendTypes.Ytdlp, true, "--remux-video", "opus"),
        new("YouTube AAC Audio (140)", "140", IsPredefined: true),
        new(FormatArg: "1080p", IsPredefined: true),
        new(FormatArg: "720p", IsPredefined: true),
        new(ContainerArg: "webm", IsPredefined: true),
        new(ContainerArg: "mp4", IsPredefined: true),
        new(ContainerArg: "mkv", IsPredefined: true),
        new(ContainerArg: "opus", IsPredefined: true),
        new(ContainerArg: "flac", IsPredefined: true),
        new(ContainerArg: "ogg", IsPredefined: true),
        new(ContainerArg: "m4a", IsPredefined: true),
        new(ContainerArg: "mp3", IsPredefined: true),
    ];
}
