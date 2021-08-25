using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.Models
{
    public class Settings : ReactiveObject
    {
        /// Gets the default configuration version
        /// used by this version of the app.
        /// </summary>
        public static int DefaultVersion => 1;

        /// <summary>
        /// Gets or sets the settings version number.
        /// Defaults to <see cref="DefaultVersion"/>.
        /// </summary>
        public int Version { get; set; } = DefaultVersion;

        public BaseTheme AppColorMode { get; set; } = BaseTheme.Inherit;

        [Reactive]
        public bool AutoUpdateDl { get; set; } = true;

        [Reactive]
        public BackendType Backend { get; set; } = BackendType.Ytdl;

        [Reactive]
        public string DlPath { get; set; } = "";

        [Reactive]
        public string FfmpegPath { get; set; } = "";

        [Reactive]
        public string Proxy { get; set; } = "";

        [Reactive]
        public string Container { get; set; } = "Auto";

        [Reactive]
        public string Format { get; set; } = "Auto";

        [Reactive]
        public bool AddMetadata { get; set; } = true;

        [Reactive]
        public bool DownloadThumbnail { get; set; } = true;

        [Reactive]
        public bool DownloadSubtitles { get; set; } = true;

        [Reactive]
        public bool DownloadPlaylist { get; set; }

        [Reactive]
        public bool UseCustomPath { get; set; }

        [Reactive]
        public string DownloadPath { get; set; } = "";

        /// <summary>
        /// Loads settings from Settings.json.
        /// </summary>
        /// <param name="cancellationToken">A token that may be used to cancel the read operation.</param>
        /// <returns>
        /// A ValueTuple containing a <see cref="Settings"/> object and an optional error message.
        /// </returns>
        public static async Task<(Settings settings, string? errMsg)> LoadSettingsAsync(CancellationToken cancellationToken = default)
        {
            var (settings, errMsg) = await FileHelper.LoadJsonAsync<Settings>("Settings.json", FileHelper.commonJsonDeserializerOptions, cancellationToken).ConfigureAwait(false);
            if (errMsg is null && settings.Version != DefaultVersion)
            {
                settings.UpdateSettings();
                errMsg = await SaveSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
            }
            return (settings, errMsg);
        }

        /// <summary>
        /// Saves settings to Settings.json.
        /// </summary>
        /// <param name="settings">The <see cref="Settings"/> object to save.</param>
        /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
        /// <returns>
        /// An optional error message.
        /// Null if no errors occurred.
        /// </returns>
        public static Task<string?> SaveSettingsAsync(Settings settings, CancellationToken cancellationToken = default)
            => FileHelper.SaveJsonAsync("Settings.json", settings, FileHelper.commonJsonSerializerOptions, false, false, cancellationToken);

        /// <summary>
        /// Updates the current object to the latest version.
        /// </summary>
        public void UpdateSettings()
        {
            switch (Version)
            {
                case 0: // nothing to do
                    Version++;
                    goto default; // go to the next update path
                default:
                    break;
            }
        }
    }
}
