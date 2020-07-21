using MaterialDesignThemes.Wpf;
using PeanutButter.TinyEventAggregator;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace youtube_dl_wpf
{
    public class HomeViewModel : ViewModelBase
    {
        public HomeViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));

            _link = "";
            _container = "Auto";
            _format = "Auto";
            _enableFormatSelection = true;
            _addMetadata = true;
            _downloadThumbnail = true;
            _downloadSubtitles = true;
            _downloadPlaylist = false;
            _useCustomPath = false;
            _downloadPath = "";
            _output = "";

            _browseFolder = new DelegateCommand(OnBrowseFolder);
            _openFolder = new DelegateCommand(OnOpenFolder, CanOpenFolder);
            _startDownload = new DelegateCommand(OnStartDownload, CanStartDownload);
            _listFormats = new DelegateCommand(OnListFormats, CanStartDownload);
            _abortDl = new DelegateCommand(OnAbortDl, (object commandParameter) => _freezeButton);

            ContainerList = new ObservableCollection<string>()
            {
                "Auto",
                "webm",
                "mp4",
                "mkv",
                "opus",
                "flac",
                "ogg",
                "m4a",
                "mp3"
            };

            FormatList = new ObservableCollection<string>()
            {
                "Auto",
                "bestvideo+bestaudio/best",
                "bestvideo+bestaudio",
                "bestvideo+worstaudio",
                "worstvideo+bestaudio",
                "worstvideo+worstaudio",
                "worstvideo+worstaudio/worst",
                "best",
                "worst",
                "bestvideo",
                "worstvideo",
                "bestaudio",
                "worstaudio",
                "YouTube 4K 60fps HDR webm (337+251)",
                "YouTube 4K 60fps webm (315+251)",
                "YouTube 4K 60fps AV1 (401+140)",
                "YouTube 4K webm (313+251)",
                "YouTube 1080p60 webm (303+251)",
                "YouTube 1080p webm (248+251)",
                "1080p",
                "720p"
            };

            settingsFromHomeEvent = EventAggregator.Instance.GetEvent<SettingsFromHomeEvent>();
            // subscribe to settings changes from SettingsViewModel
            EventAggregator.Instance.GetEvent<SettingsChangedEvent>().Subscribe(x =>
            {
                _settings = x;
                ApplySettings();
            });
        }

        private SettingsJson _settings = null!;
        private bool _updated;
        private readonly SettingsFromHomeEvent settingsFromHomeEvent;

        private string _link;
        private string _container;
        private string _format;
        private bool _enableFormatSelection;
        private bool _addMetadata;
        private bool _downloadThumbnail;
        private bool _downloadSubtitles;
        private bool _downloadPlaylist;
        private bool _useCustomPath;
        private string _downloadPath;
        private string _output;

        private bool _freezeButton; // true when youtube-dl is started
        private StringBuilder outputString = null!;
        private Process dlProcess = null!;

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly DelegateCommand _browseFolder;
        private readonly DelegateCommand _openFolder;
        private readonly DelegateCommand _startDownload;
        private readonly DelegateCommand _listFormats;
        private readonly DelegateCommand _abortDl;

        public ICommand BrowseFolder => _browseFolder;
        public ICommand OpenFolder => _openFolder;
        public ICommand StartDownload => _startDownload;
        public ICommand ListFormats => _listFormats;
        public ICommand AbortDl => _abortDl;

        /// <summary>
        /// Apply new settings published by SettingsViewModel.
        /// </summary>
        private void ApplySettings()
        {
            SetProperty(ref _container, _settings.Container);
            SetProperty(ref _format, _settings.Format);
            SetProperty(ref _addMetadata, _settings.AddMetadata);
            SetProperty(ref _downloadThumbnail, _settings.DownloadThumbnail);
            SetProperty(ref _downloadSubtitles, _settings.DownloadSubtitles);
            SetProperty(ref _downloadPlaylist, _settings.DownloadPlaylist);
            SetProperty(ref _useCustomPath, _settings.UseCustomPath);
            SetProperty(ref _downloadPath, _settings.DownloadPath);

            if (_container == "Auto")
                EnableFormatSelection = true;
            else
                EnableFormatSelection = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _openFolder.InvokeCanExecuteChanged();
                _startDownload.InvokeCanExecuteChanged();
                _listFormats.InvokeCanExecuteChanged();
                if (!_updated && !String.IsNullOrEmpty(_settings.DlPath) && _settings.AutoUpdateDl)
                {
                    UpdateDl();
                }
                _updated = true;
            });
        }

        /// <summary>
        /// Publish settings to SettingsViewModel.
        /// </summary>
        private void PublishSettings() => Task.Run(() => settingsFromHomeEvent.PublishAsync(_settings));

        /// <summary>
        /// Initialize dlProcess with common properties.
        /// </summary>
        private void PrepareDlProcess()
        {
            dlProcess = new Process();
            dlProcess.StartInfo.FileName = _settings.DlPath;
            dlProcess.StartInfo.CreateNoWindow = true;
            dlProcess.StartInfo.UseShellExecute = false;
            dlProcess.StartInfo.RedirectStandardError = true;
            dlProcess.StartInfo.RedirectStandardOutput = true;
            dlProcess.EnableRaisingEvents = true;
            dlProcess.ErrorDataReceived += DlOutputHandler;
            dlProcess.OutputDataReceived += DlOutputHandler;
            dlProcess.Exited += DlProcess_Exited;
        }

        private void UpdateButtons()
        {
            _startDownload.InvokeCanExecuteChanged();
            _listFormats.InvokeCanExecuteChanged();
            _abortDl.InvokeCanExecuteChanged();
        }

        private void DlProcess_Exited(object? sender, EventArgs e)
        {
            dlProcess.Dispose();
            FreezeButton = false;
            Application.Current.Dispatcher.Invoke(UpdateButtons);
        }

        private void OnBrowseFolder(object commandParameter)
        {
            Microsoft.Win32.OpenFileDialog folderDialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Folder Selection.",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true
            };

            if ((string)commandParameter == "DownloadPath")
                folderDialog.InitialDirectory = DownloadPath;

            bool? result = folderDialog.ShowDialog();

            if (result == true)
            {
                if ((string)commandParameter == "DownloadPath")
                    DownloadPath = Path.GetDirectoryName(folderDialog.FileName) ?? "";
            }
        }

        private void OnOpenFolder(object commandParameter)
        {
            try
            {
                Utilities.OpenLink(_downloadPath);
            }
            catch (Exception ex)
            {
                Output = ex.Message;
            }
        }

        private void OnStartDownload(object commandParameter)
        {
            FreezeButton = true;
            UpdateButtons();

            outputString = new StringBuilder();
            PrepareDlProcess();

            try
            {
                // make parameter list
                if (!String.IsNullOrEmpty(_settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{_settings.Proxy}");
                }
                if (!String.IsNullOrEmpty(_settings.FfmpegPath))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--ffmpeg-location");
                    dlProcess.StartInfo.ArgumentList.Add($"{_settings.FfmpegPath}");
                }
                if (_container != "Auto")
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{_container}");
                }
                else if (_format != "Auto")
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{_format}");
                }
                if (_addMetadata)
                    dlProcess.StartInfo.ArgumentList.Add("--add-metadata");
                if (_downloadThumbnail)
                    dlProcess.StartInfo.ArgumentList.Add("--embed-thumbnail");
                if (_downloadSubtitles)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--write-sub");
                    dlProcess.StartInfo.ArgumentList.Add("--embed-subs");
                }
                if (_downloadPlaylist)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--yes-playlist");
                }
                else
                {
                    dlProcess.StartInfo.ArgumentList.Add("--no-playlist");
                }
                if (_useCustomPath)
                {
                    dlProcess.StartInfo.ArgumentList.Add("-o");
                    dlProcess.StartInfo.ArgumentList.Add($@"{_downloadPath}\%(title)s-%(id)s.%(ext)s");
                }
                dlProcess.StartInfo.ArgumentList.Add($"{_link}");
                // start download
                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            finally
            {
            }
        }

        private void OnListFormats(object commandParameter)
        {
            FreezeButton = true;
            UpdateButtons();

            outputString = new StringBuilder();
            PrepareDlProcess();

            try
            {
                // make parameter list
                if (!String.IsNullOrEmpty(_settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{_settings.Proxy}");
                }
                dlProcess.StartInfo.ArgumentList.Add($"-F");
                dlProcess.StartInfo.ArgumentList.Add($"{_link}");
                // start download
                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            finally
            {
            }
        }

        private void OnAbortDl(object commandParameter)
        {
            try
            {
                // yes, I know it's bad to just kill the process.
                // but currently .NET Core doesn't have an API for sending ^C or SIGTERM to a process
                // see https://github.com/dotnet/runtime/issues/14628
                // To implement a platform-specific solution,
                // we need to use Win32 APIs.
                // see https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
                // I would prefer not to use Win32 APIs in the application.
                dlProcess.Kill();
                outputString.Append("🛑 Aborted.");
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            catch (Exception ex)
            {
                Output = ex.Message;
            }
        }

        private bool CanOpenFolder(object commandParameter)
        {
            return !String.IsNullOrEmpty(_downloadPath) && Directory.Exists(_downloadPath);
        }

        private bool CanStartDownload(object commandParameter)
        {
            return !String.IsNullOrEmpty(Link) && !String.IsNullOrEmpty(_settings.DlPath) && !_freezeButton;
        }

        private void UpdateDl()
        {
            FreezeButton = true;
            UpdateButtons();

            outputString = new StringBuilder();
            PrepareDlProcess();

            try
            {
                // make parameter list
                if (!String.IsNullOrEmpty(_settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{_settings.Proxy}");
                }
                dlProcess.StartInfo.ArgumentList.Add($"-U");
                // start update
                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            finally
            {
            }
        }

        private void DlOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                outputString.Append(outLine.Data);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
        }

        public string Link
        {
            get => _link;
            set
            {
                SetProperty(ref _link, value);
                _startDownload.InvokeCanExecuteChanged();
                _listFormats.InvokeCanExecuteChanged();
                if (String.IsNullOrEmpty(_settings.DlPath))
                    _snackbarMessageQueue.Enqueue("youtube-dl path is not set. Go to settings and set the path.");
            }
        }

        public ObservableCollection<string> ContainerList { get; }

        public string Container
        {
            get => _container;
            set
            {
                SetProperty(ref _container, value);
                if (_container == "Auto")
                    EnableFormatSelection = true;
                else
                {
                    EnableFormatSelection = false;
                    Format = "Auto";
                }
                _settings.Container = _container;
                PublishSettings();
            }
        }

        public ObservableCollection<string> FormatList { get; }

        public string Format
        {
            get => _format;
            set
            {
                SetProperty(ref _format, value);
                _settings.Format = _format;
                PublishSettings();
            }
        }

        public bool EnableFormatSelection
        {
            get => _enableFormatSelection;
            set => SetProperty(ref _enableFormatSelection, value);
        }

        public bool AddMetadata
        {
            get => _addMetadata;
            set
            {
                SetProperty(ref _addMetadata, value);
                _settings.AddMetadata = _addMetadata;
                PublishSettings();
            }
        }

        public bool DownloadThumbnail
        {
            get => _downloadThumbnail;
            set
            {
                SetProperty(ref _downloadThumbnail, value);
                _settings.DownloadThumbnail = _downloadThumbnail;
                PublishSettings();
            }
        }

        public bool DownloadSubtitles
        {
            get => _downloadSubtitles;
            set
            {
                SetProperty(ref _downloadSubtitles, value);
                _settings.DownloadSubtitles = _downloadSubtitles;
                PublishSettings();
            }
        }

        public bool DownloadPlaylist
        {
            get => _downloadPlaylist;
            set
            {
                SetProperty(ref _downloadPlaylist, value);
                _settings.DownloadPlaylist = _downloadPlaylist;
                PublishSettings();
            }
        }

        public bool UseCustomPath
        {
            get => _useCustomPath;
            set
            {
                SetProperty(ref _useCustomPath, value);
                _settings.UseCustomPath = _useCustomPath;
                PublishSettings();
            }
        }

        public string DownloadPath
        {
            get => _downloadPath;
            set
            {
                SetProperty(ref _downloadPath, value);
                _openFolder.InvokeCanExecuteChanged();
                _settings.DownloadPath = _downloadPath;
                PublishSettings();
            }
        }

        public string Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }

        public bool FreezeButton
        {
            get => _freezeButton;
            set => SetProperty(ref _freezeButton, value);
        }
    }

    /// <summary>
    /// Raised by HomeViewModel when settings are changed.
    /// </summary>
    public class SettingsFromHomeEvent : EventBase<SettingsJson>
    {
    }
}
