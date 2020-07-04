using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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
            CustomPath = false;
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
        public bool CustomPath { get; set; }
        public string DownloadPath { get; set; }
    }

    public static class AppSettings
    {
        public static SettingsJson settings = null!;

        static AppSettings()
        {
            // Is it a good idea to do it here?
            //LoadSettingsAsync().Wait();
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (!File.Exists("Settings.json"))
            {
                settings = new SettingsJson();
                return;
            }
            try
            {
                var _settingsJson = new FileStream("Settings.json", FileMode.Open);
                settings = JsonSerializer.DeserializeAsync<SettingsJson>(_settingsJson).Result;
            }
            catch
            {
                settings = new SettingsJson();
            }
        }

        public static async Task LoadSettingsAsync()
        {
            if (!File.Exists("Settings.json"))
            {
                settings = new SettingsJson();
                return;
            }
            try
            {
                var _settingsJson = new FileStream("Settings.json", FileMode.Open);
                settings = await JsonSerializer.DeserializeAsync<SettingsJson>(_settingsJson);
            }
            catch
            {
                settings = new SettingsJson();
            }
        }

        public static void SaveSettings()
        {
            Task.Run(SaveSettingsAsync);
        }

        public static async Task SaveSettingsAsync()
        {
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            using var _settingsJson = new FileStream("Settings.json", FileMode.Create);
            await JsonSerializer.SerializeAsync(_settingsJson, settings!, jsonSerializerOptions);
        }
    }
}
