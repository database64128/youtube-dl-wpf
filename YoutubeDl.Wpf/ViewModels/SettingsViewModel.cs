using MaterialDesignThemes.Wpf;
using PeanutButter.TinyEventAggregator;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));

            _followOSColorMode = true;
            _lightMode = false;
            _darkMode = false;
            _autoUpdateDl = true;
            _dlPath = "";
            _ffmpegPath = "";
            _proxy = "";

            _paletteHelper = new PaletteHelper();
            _changeColorMode = new DelegateCommand(OnChangeColorMode);
            _browseExe = new DelegateCommand(OnBrowseExe);

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

        private SettingsJson _settings = null!;
        private readonly SettingsChangedEvent settingsChangedEvent;

        private bool _followOSColorMode; // default to true
        private bool _lightMode;
        private bool _darkMode;
        private bool _autoUpdateDl; // auto update youtube-dl by default
        private string _dlPath; // youtube-dl path
        private string _ffmpegPath;
        private string _proxy;

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly PaletteHelper _paletteHelper;
        private readonly DelegateCommand _changeColorMode;
        private readonly DelegateCommand _browseExe;

        public ICommand ChangeColorMode => _changeColorMode;
        public ICommand BrowseExe => _browseExe;

        private void OnChangeColorMode(object commandParameter)
        {
            ITheme theme = _paletteHelper.GetTheme();
            switch (commandParameter)
            {
                case ColorMode.System:
                    var systemTheme = Theme.GetSystemTheme();
                    switch (systemTheme)
                    {
                        case BaseTheme.Dark:
                            theme.SetBaseTheme(Theme.Dark);
                            break;
                        case BaseTheme.Light:
                        default:
                            theme.SetBaseTheme(Theme.Light);
                            break;
                    }
                    break;
                case ColorMode.Light:
                    theme.SetBaseTheme(Theme.Light);
                    break;
                case ColorMode.Dark:
                    theme.SetBaseTheme(Theme.Dark);
                    break;
                default:
                    throw new ArgumentException("Invalid AppColorMode");
            }
            _paletteHelper.SetTheme(theme);
            if (_settings.AppColorMode != (ColorMode)commandParameter)
            {
                _settings.AppColorMode = (ColorMode)commandParameter;
                SaveSettings();
            }
        }

        private void OnBrowseExe(object commandParameter)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = (string)commandParameter,
                DefaultExt = ".exe",
                Filter = "Executables (.exe)|*.exe"
            };

            if ((string)commandParameter == "youtube-dl")
                openFileDialog.InitialDirectory = Path.GetDirectoryName(_dlPath);
            else if ((string)commandParameter == "ffmpeg")
                openFileDialog.InitialDirectory = Path.GetDirectoryName(_ffmpegPath);
            
            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                if ((string)commandParameter == "youtube-dl")
                    DlPath = openFileDialog.FileName;
                else if ((string)commandParameter == "ffmpeg")
                    FfmpegPath = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// Load settings from Settings.json and save to the _settings field.
        /// </summary>
        /// <returns></returns>
        private async Task LoadSettingsAsync()
        {
            if (!File.Exists("Settings.json"))
            {
                _settings = new SettingsJson();
            }
            else
            {
                FileStream _settingsJson = null!;
                try
                {
                    _settingsJson = new FileStream("Settings.json", FileMode.Open);
                    _settings = await JsonSerializer.DeserializeAsync<SettingsJson>(_settingsJson);
                }
                catch
                {
                    _settings = new SettingsJson();
                    _snackbarMessageQueue.Enqueue("Failed to load settings. All settings have been reset.");
                }
                finally
                {
                    if (_settingsJson != null)
                        await _settingsJson.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Apply loaded settings from _settings.
        /// </summary>
        /// <returns></returns>
        private async Task ApplySettings()
        {
            switch (_settings.AppColorMode)
            {
                case ColorMode.System:
                    SetProperty(ref _followOSColorMode, true);
                    SetProperty(ref _lightMode, false);
                    SetProperty(ref _darkMode, false);
                    break;
                case ColorMode.Light:
                    SetProperty(ref _followOSColorMode, false);
                    SetProperty(ref _lightMode, true);
                    SetProperty(ref _darkMode, false);
                    break;
                case ColorMode.Dark:
                    SetProperty(ref _followOSColorMode, false);
                    SetProperty(ref _lightMode, false);
                    SetProperty(ref _darkMode, true);
                    break;
                default:
                    throw new ArgumentException("Invalid AppColorMode");
            }
            OnChangeColorMode(_settings.AppColorMode);
            SetProperty(ref _autoUpdateDl, _settings.AutoUpdateDl);
            SetProperty(ref _dlPath, _settings.DlPath);
            SetProperty(ref _ffmpegPath, _settings.FfmpegPath);
            SetProperty(ref _proxy, _settings.Proxy);

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
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            FileStream _settingsJson = null!;
            try
            {
                _settingsJson = new FileStream("Settings.json", FileMode.Create);
                await JsonSerializer.SerializeAsync(_settingsJson, _settings, jsonSerializerOptions);
            }
            catch
            {
                _snackbarMessageQueue.Enqueue("Failed to save settings. Please check the executable directory's permissions.");
            }
            finally
            {
                if (_settingsJson != null)
                    await _settingsJson.DisposeAsync();
            }
        }

        public bool FollowOSColorMode
        {
            get => _followOSColorMode;
            set => SetProperty(ref _followOSColorMode, value);
        }

        public bool LightMode
        {
            get => _lightMode;
            set => SetProperty(ref _lightMode, value);
        }

        public bool DarkMode
        {
            get => _darkMode;
            set => SetProperty(ref _darkMode, value);
        }

        public bool AutoUpdateDl
        {
            get => _autoUpdateDl;
            set
            {
                SetProperty(ref _autoUpdateDl, value);
                _settings.AutoUpdateDl = _autoUpdateDl;
                SaveSettings();
            }
        }

        public string DlPath
        {
            get => _dlPath;
            set
            {
                SetProperty(ref _dlPath, value);
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
                SetProperty(ref _ffmpegPath, value);
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
                SetProperty(ref _proxy, value);
                _settings.Proxy = _proxy;
                SaveSettings();
                PublishSettings();
            }
        }
    }

    /// <summary>
    /// Raised by SettingsViewModel when settings are loaded or changed in SettingsViewModel.
    /// </summary>
    public class SettingsChangedEvent : EventBase<SettingsJson>
    {
    }
}
