using MaterialDesignThemes.Wpf;
using PeanutButter.TinyEventAggregator;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace youtube_dl_wpf
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));

            _darkMode = false;
            _autoUpdateDl = true;
            _colorMode = "Light Mode";
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

        private bool _darkMode; // default to light mode
        private bool _autoUpdateDl; // auto update youtube-dl by default
        private string _colorMode; // color mode text for TextBlock
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
            IBaseTheme baseTheme = (bool)commandParameter ? new MaterialDesignDarkTheme() : (IBaseTheme)new MaterialDesignLightTheme();
            theme.SetBaseTheme(baseTheme);
            _paletteHelper.SetTheme(theme);
            ColorMode = (bool)commandParameter ? "Dark Mode" : "Light Mode";
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
            SetProperty(ref _darkMode, _settings.DarkMode);
            SetProperty(ref _autoUpdateDl, _settings.AutoUpdateDl);
            SetProperty(ref _dlPath, _settings.DlPath);
            SetProperty(ref _ffmpegPath, _settings.FfmpegPath);
            SetProperty(ref _proxy, _settings.Proxy);

            if (_darkMode == true)
                OnChangeColorMode(true);

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

        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                SetProperty(ref _darkMode, value);
                _settings.DarkMode = _darkMode;
                SaveSettings();
                PublishSettings();
            }
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

        public string ColorMode
        {
            get => _colorMode;
            set => SetProperty(ref _colorMode, value);
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
