using System;
using System.Collections.Generic;
using System.Text;

namespace youtube_dl_wpf
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool _darkMode; // default to false (light mode)
        private string _colorMode = "Light Mode"; // color mode text for TextBlock
        private string _dlPath; // youtube-dl path
        private string _ffmpegPath;
        private string _proxy;

        public string ColorMode
        {
            get => _colorMode;
            set => SetProperty(ref _colorMode, value);
        }

        public bool DarkMode
        {
            get => _darkMode;
            set => SetProperty(ref _darkMode, value);
        }

        public string DlPath
        {
            get => _dlPath;
            set => SetProperty(ref _dlPath, value);
        }

        public string FfmpegPath
        {
            get => _ffmpegPath;
            set => SetProperty(ref _ffmpegPath, value);
        }

        public string Proxy
        {
            get => _proxy;
            set => SetProperty(ref _proxy, value);
        }
    }
}
