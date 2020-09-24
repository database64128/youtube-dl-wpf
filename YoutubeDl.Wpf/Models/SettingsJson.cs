namespace YoutubeDl.Wpf.Models
{
    public class SettingsJson
    {
        public SettingsJson()
        {
            AppColorMode = ColorMode.System;
            AutoUpdateDl = true;
            DlPath = "";
            FfmpegPath = "";
            Proxy = "";

            Container = "Auto";
            Format = "Auto";
            AddMetadata = true;
            DownloadThumbnail = true;
            DownloadSubtitles = true;
            DownloadPlaylist = false;
            UseCustomPath = false;
            DownloadPath = "";
        }
        
        public ColorMode AppColorMode { get; set; }
        public bool AutoUpdateDl { get; set; }
        public string DlPath { get; set; }
        public string FfmpegPath { get; set; }
        public string Proxy { get; set; }

        public string Container { get; set; }
        public string Format { get; set; }
        public bool AddMetadata { get; set; }
        public bool DownloadThumbnail { get; set; }
        public bool DownloadSubtitles { get; set; }
        public bool DownloadPlaylist { get; set; }
        public bool UseCustomPath { get; set; }
        public string DownloadPath { get; set; }
    }

    public enum ColorMode
    {
        System,
        Light,
        Dark
    }
}
