using System;

namespace YoutubeDl.Wpf.Models;

[Flags]
public enum BackendTypes
{
    None = 0,
    Ytdl = 1,
    Ytdlp = 2,
}

public static class BackendTypesExtensions
{
    public static string ToExecutableName(this BackendTypes backend) => backend switch
    {
        BackendTypes.Ytdl => "youtube-dl",
        BackendTypes.Ytdlp => "yt-dlp",
        _ => "",
    };
}
