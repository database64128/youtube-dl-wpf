namespace youtube_dl_wpf
{
    public class SettingsJson
    {
        public SettingsJson()
        {
            // define default settings
            DarkMode = false;
            AutoUpdateDl = true;
            DlPath = "";
            FfmpegPath = "";
            Proxy = "";

            OverrideFormats = false;
            VideoFormat = "248";
            AudioFormat = "251";
            AddMetadata = true;
            DownloadThumbnail = true;
            DownloadSubtitles = true;
            DownloadPlaylist = false;
            UseCustomPath = false;
            DownloadPath = "";
        }
        
        public bool DarkMode { get; set; }
        public bool AutoUpdateDl { get; set; }
        public string DlPath { get; set; }
        public string FfmpegPath { get; set; }
        public string Proxy { get; set; }

        public bool OverrideFormats { get; set; }
        public string VideoFormat { get; set; }
        public string AudioFormat { get; set; }
        public bool AddMetadata { get; set; }
        public bool DownloadThumbnail { get; set; }
        public bool DownloadSubtitles { get; set; }
        public bool DownloadPlaylist { get; set; }
        public bool UseCustomPath { get; set; }
        public string DownloadPath { get; set; }
    }
}
