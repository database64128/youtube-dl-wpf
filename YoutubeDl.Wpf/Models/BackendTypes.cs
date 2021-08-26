using System;

namespace YoutubeDl.Wpf.Models
{
    [Flags]
    public enum BackendTypes
    {
        None = 0,
        Ytdl = 1,
        Ytdlp = 2,
    }
}
