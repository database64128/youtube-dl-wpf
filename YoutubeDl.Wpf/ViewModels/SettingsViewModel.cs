using MaterialDesignThemes.Wpf;
using PeanutButter.TinyEventAggregator;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.IO;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.ViewModels
{
    public class SettingsViewModel : ReactiveValidationObject
    {
        public SettingsViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));

            FollowOSColorMode = true;
            LightMode = false;
            DarkMode = false;
            _autoUpdateDl = true;
            _dlPath = "";
            _ffmpegPath = "";
            _proxy = "";

            Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "";

            _paletteHelper = new PaletteHelper();

            this.ValidationRule(
                viewModel => viewModel.DlPath,
                dlPath => File.Exists(dlPath),
                "Invalid youtube-dl binary path.");

            this.ValidationRule(
                viewModel => viewModel.FfmpegPath,
                ffmpegPath => string.IsNullOrEmpty(ffmpegPath) || File.Exists(ffmpegPath),
                "Invalid ffmpeg binary path.");

            this.ValidationRule(
                viewModel => viewModel.Proxy,
                proxy => string.IsNullOrEmpty(proxy) || (Uri.TryCreate(proxy, UriKind.Absolute, out var uri) && (uri.Scheme == "socks5"
                                                                                                                 || uri.Scheme == "http"
                                                                                                                 || uri.Scheme == "https")),
                "Invalid proxy URL.");

            ChangeColorModeToSystem = ReactiveCommand.Create(() => OnChangeColorMode(BaseTheme.Inherit));
            ChangeColorModeToLight = ReactiveCommand.Create(() => OnChangeColorMode(BaseTheme.Light));
            ChangeColorModeToDark = ReactiveCommand.Create(() => OnChangeColorMode(BaseTheme.Dark));
            BrowseDlBinaryCommand = ReactiveCommand.Create(BrowseDlBinary);
            BrowseFfmpegBinaryCommand = ReactiveCommand.Create(BrowseFfmpegBinary);
            OpenUri = ReactiveCommand.Create<string>(uri => WpfHelper.OpenUri(uri));

            settingsChangedEvent = EventAggregator.Instance.GetEvent<SettingsChangedEvent>();
            // subscribe to settings changes published by HomeViewModel
            EventAggregator.Instance.GetEvent<SettingsFromHomeEvent>().Subscribe(async x =>
            {
                _settings = x;
                await SaveSettingsAsync();
            });
            // load and apply settings from json
            Task.Run(LoadSettingsAsync).ContinueWith(x => ApplySettings());
        }

        private Settings _settings = null!;
        private readonly SettingsChangedEvent settingsChangedEvent;

        private bool _autoUpdateDl; // auto update youtube-dl by default
        private string _dlPath; // youtube-dl path
        private string _ffmpegPath;
        private string _proxy;

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly PaletteHelper _paletteHelper;

        public ReactiveCommand<Unit, Unit> ChangeColorModeToSystem { get; }
        public ReactiveCommand<Unit, Unit> ChangeColorModeToLight { get; }
        public ReactiveCommand<Unit, Unit> ChangeColorModeToDark { get; }
        public ReactiveCommand<Unit, Unit> BrowseDlBinaryCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseFfmpegBinaryCommand { get; }
        public ReactiveCommand<string, Unit> OpenUri { get; }

        private void OnChangeColorMode(BaseTheme colorMode)
        {
            // Get current theme.
            var theme = _paletteHelper.GetTheme();

            // Get current system theme if required.
            var targetColorMode = colorMode switch
            {
                BaseTheme.Inherit => Theme.GetSystemTheme() ?? BaseTheme.Light,
                _ => colorMode,
            };

            // Apply base theme
            switch (targetColorMode)
            {
                case BaseTheme.Light:
                    theme.SetBaseTheme(Theme.Light);
                    break;
                case BaseTheme.Dark:
                    theme.SetBaseTheme(Theme.Dark);
                    break;
            }

            // Apply theme
            _paletteHelper.SetTheme(theme);

            // Save setting if changed.
            if (_settings.AppColorMode != colorMode)
            {
                _settings.AppColorMode = colorMode;
                SaveSettings();
            }
        }

        private void BrowseDlBinary() => DlPath = BrowseBinary("youtube-dl", DlPath);

        private void BrowseFfmpegBinary() => FfmpegPath = BrowseBinary("ffmpeg", FfmpegPath);

        private static string BrowseBinary(string filename, string path)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                FileName = filename,
                DefaultExt = ".exe",
                Filter = "Executables (.exe)|*.exe",
                InitialDirectory = Path.GetDirectoryName(path),
            };

            var result = openFileDialog.ShowDialog();
            return result == true ? openFileDialog.FileName : path;
        }

        /// <summary>
        /// Load settings from Settings.json and save to the _settings field.
        /// </summary>
        /// <returns></returns>
        private async Task LoadSettingsAsync()
        {
            (var settings, var errMsg) = await Settings.LoadSettingsAsync();
            if (errMsg is not null)
                _snackbarMessageQueue.Enqueue(errMsg);

            _settings = settings;
        }

        /// <summary>
        /// Apply loaded settings from _settings.
        /// </summary>
        /// <returns></returns>
        private async Task ApplySettings()
        {
            switch (_settings.AppColorMode)
            {
                case BaseTheme.Inherit:
                    FollowOSColorMode = true;
                    LightMode = false;
                    DarkMode = false;
                    break;
                case BaseTheme.Light:
                    FollowOSColorMode = false;
                    LightMode = true;
                    DarkMode = false;
                    break;
                case BaseTheme.Dark:
                    FollowOSColorMode = false;
                    LightMode = false;
                    DarkMode = true;
                    break;
            }

            OnChangeColorMode(_settings.AppColorMode);

            this.RaiseAndSetIfChanged(ref _autoUpdateDl, _settings.AutoUpdateDl);
            this.RaiseAndSetIfChanged(ref _dlPath, _settings.DlPath);
            this.RaiseAndSetIfChanged(ref _ffmpegPath, _settings.FfmpegPath);
            this.RaiseAndSetIfChanged(ref _proxy, _settings.Proxy);

            await settingsChangedEvent.PublishAsync(_settings);
        }

        /// <summary>
        /// Publish _settings for other ViewModels.
        /// </summary>
        private void PublishSettings() => Task.Run(() => settingsChangedEvent.PublishAsync(_settings));

        private void SaveSettings() => Task.Run(SaveSettingsAsync);

        /// <summary>
        /// Serialize _settings to Settings.json.
        /// </summary>
        /// <returns></returns>
        private async Task SaveSettingsAsync()
        {
            var errMsg = await Settings.SaveSettingsAsync(_settings);
            if (errMsg is not null)
                _snackbarMessageQueue.Enqueue(errMsg);
        }

        [Reactive]
        public bool FollowOSColorMode { get; set; }

        [Reactive]
        public bool LightMode { get; set; }

        [Reactive]
        public bool DarkMode { get; set; }

        public bool AutoUpdateDl
        {
            get => _autoUpdateDl;
            set
            {
                this.RaiseAndSetIfChanged(ref _autoUpdateDl, value);
                _settings.AutoUpdateDl = _autoUpdateDl;
                SaveSettings();
            }
        }

        public string DlPath
        {
            get => _dlPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _dlPath, value);
                _settings.DlPath = _dlPath;
                SaveSettings();
                PublishSettings();
            }
        }

        public string FfmpegPath
        {
            get => _ffmpegPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _ffmpegPath, value);
                _settings.FfmpegPath = _ffmpegPath;
                SaveSettings();
                PublishSettings();
            }
        }

        public string Proxy
        {
            get => _proxy;
            set
            {
                this.RaiseAndSetIfChanged(ref _proxy, value);
                _settings.Proxy = _proxy;
                SaveSettings();
                PublishSettings();
            }
        }

        public string Version { get; }
    }

    /// <summary>
    /// Raised by SettingsViewModel when settings are loaded or changed in SettingsViewModel.
    /// </summary>
    public class SettingsChangedEvent : EventBase<Settings>
    {
    }
}
