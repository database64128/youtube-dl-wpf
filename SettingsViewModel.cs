using MaterialDesignThemes.Wpf;
using System;
using System.IO;
using System.Windows.Input;

namespace youtube_dl_wpf
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel()
        {
            _darkMode = AppSettings.settings!.DarkMode;
            _autoUpdateDl = AppSettings.settings.AutoUpdateDl;
            _colorMode = "Light Mode";
            _dlPath = AppSettings.settings.DlPath;
            _ffmpegPath = AppSettings.settings.FfmpegPath;
            _proxy = AppSettings.settings.Proxy;

            _paletteHelper = new PaletteHelper();
            _changeColorMode = new DelegateCommand(OnChangeColorMode, (object commandParameter) => true);
            _browseExe = new DelegateCommand(OnBrowseExe, (object commandParameter) => true);

            if (_darkMode == true)
                OnChangeColorMode(true);
        }

        private bool _darkMode; // default to light mode
        private bool _autoUpdateDl; // auto update youtube-dl by default
        private string _colorMode; // color mode text for TextBlock
        private string _dlPath; // youtube-dl path
        private string _ffmpegPath;
        private string _proxy;

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

        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                SetProperty(ref _darkMode, value);
                AppSettings.settings!.DarkMode = _darkMode;
                AppSettings.SaveSettings();
            }
        }

        public bool AutoUpdateDl
        {
            get => _autoUpdateDl;
            set
            {
                SetProperty(ref _autoUpdateDl, value);
                AppSettings.settings!.AutoUpdateDl = _autoUpdateDl;
                AppSettings.SaveSettings();
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
                AppSettings.settings!.DlPath = _dlPath;
                AppSettings.SaveSettings();
            }
        }

        public string FfmpegPath
        {
            get => _ffmpegPath;
            set
            {
                SetProperty(ref _ffmpegPath, value);
                AppSettings.settings!.FfmpegPath = _ffmpegPath;
                AppSettings.SaveSettings();
            }
        }

        public string Proxy
        {
            get => _proxy;
            set
            {
                SetProperty(ref _proxy, value);
                AppSettings.settings!.Proxy = _proxy;
                AppSettings.SaveSettings();
            }
        }
    }
}
