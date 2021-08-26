namespace YoutubeDl.Wpf.Models
{
    public record Format(string? Name, string? FormatArg, string? ContainerArg = null, BackendTypes SupportedBackends = BackendTypes.Ytdl | BackendTypes.Ytdlp, params string[] ExtraArgs)
    {
        public string DisplayName => Name ?? FormatArg ?? ContainerArg ?? "Auto";

        public static Format Auto => new(null, null, null);

        public static Format[] PredefinedFormats => new Format[]
        {
            Auto,
            new(null, "bestvideo+bestaudio/best", null),
            new(null, "bestvideo+bestaudio", null),
            new(null, "bestvideo+worstaudio", null),
            new(null, "worstvideo+bestaudio", null),
            new(null, "worstvideo+worstaudio", null),
            new(null, "worstvideo+worstaudio/worst", null),
            new(null, "best", null),
            new(null, "worst", null),
            new(null, "bestvideo", null),
            new(null, "worstvideo", null),
            new(null, "bestaudio", null),
            new(null, "worstaudio", null),
            new("YouTube 4K 60fps HDR AV1 + Opus WebM (701+251)", "701+251", "webm"),
            new("YouTube 4K 60fps HDR VP9 + Opus WebM (337+251)", "337+251", null),
            new("YouTube 4K 60fps AV1 + Opus WebM (401+251)", "401+251", "webm"),
            new("YouTube 4K 60fps VP9 + Opus WebM (315+251)", "315+251", null),
            new("YouTube 4K AV1 + Opus WebM (401+251)", "401+251", "webm"),
            new("YouTube 4K VP9 + Opus WebM (313+251)", "313+251", null),
            new("YouTube 1440p60 HDR AV1 + Opus WebM (700+251)", "700+251", "webm"),
            new("YouTube 1440p60 HDR VP9 + Opus WebM (336+251)", "336+251", null),
            new("YouTube 1440p60 AV1 + Opus WebM (400+251)", "400+251", "webm"),
            new("YouTube 1440p60 VP9 + Opus WebM (308+251)", "308+251", null),
            new("YouTube 1440p AV1 + Opus WebM (400+251)", "400+251", "webm"),
            new("YouTube 1440p VP9 + Opus WebM (271+251)", "271+251", null),
            new("YouTube 1080p60 AV1 + Opus WebM (399+251)", "399+251", "webm"),
            new("YouTube 1080p60 VP9 + Opus WebM (303+251)", "303+251", null),
            new("YouTube 1080p60 AVC + AAC MP4 (299+140)", "299+140", null),
            new("YouTube 1080p AV1 + Opus WebM (399+251)", "399+251", "webm"),
            new("YouTube 1080p VP9 + Opus WebM (248+251)", "248+251", null),
            new("YouTube 1080p AVC + AAC MP4 (137+140)", "137+140", null),
            new("YouTube 720p60 AV1 + Opus WebM (398+251)", "398+251", "webm"),
            new("YouTube 720p60 VP9 + Opus WebM (302+251)", "302+251", null),
            new("YouTube 720p60 AVC + AAC MP4 (298+140)", "298+140", null),
            new("YouTube 720p AV1 + Opus WebM (398+251)", "398+251", "webm"),
            new("YouTube 720p VP9 + Opus WebM (247+251)", "247+251", null),
            new("YouTube 720p AVC + AAC (136+140)", "136+140", null),
            new("YouTube Opus Audio (251)", "251", null, BackendTypes.Ytdlp, "--remux-video", "opus"),
            new("YouTube AAC Audio (140)", "140", null),
            new(null, "1080p", null),
            new(null, "720p", null),
        };

        public static Format[] PredefinedContainers => new Format[]
        {
            Auto,
            new(null, null, "webm"),
            new(null, null, "mp4"),
            new(null, null, "mkv"),
            new(null, null, "opus"),
            new(null, null, "flac"),
            new(null, null, "ogg"),
            new(null, null, "m4a"),
            new(null, null, "mp3"),
        };
    }
}
