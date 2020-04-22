using System;
using System.Collections.Generic;
using System.Text;

namespace youtube_dl_wpf
{
    public class HomeViewModel : ViewModelBase
    {
        private string _link;
        private bool _listFormats;
        private bool _overrideFormats;
        private string _videoFormat;
        private string _audioFormat;
        private bool _thumbnail = true;
        private bool _subtitles = true;
        private bool _customPath;
        private string _path;
        private string _output;

        public string Link
        {
            get => _link;
            set => SetProperty(ref _link, value);
        }

        public bool ListFormats
        {
            get => _listFormats;
            set => SetProperty(ref _listFormats, value);
        }

        public bool OverrideFormats
        {
            get => _overrideFormats;
            set => SetProperty(ref _overrideFormats, value);
        }

        public string VideoFormat
        {
            get => _videoFormat;
            set => SetProperty(ref _videoFormat, value);
        }

        public string AudioFormat
        {
            get => _audioFormat;
            set => SetProperty(ref _audioFormat, value);
        }

        public bool Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        public bool Subtitles
        {
            get => _subtitles;
            set => SetProperty(ref _subtitles, value);
        }

        public bool CustomPath
        {
            get => _customPath;
            set => SetProperty(ref _customPath, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public string Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }
    }
}
