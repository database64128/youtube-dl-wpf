using MaterialDesignThemes.Wpf;
using PeanutButter.TinyEventAggregator;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.ViewModels
{
    public class HomeViewModel : ReactiveValidationObject
    {
        public HomeViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue;
            _settings = new();

            DlBinaryPath = "";
            _link = "";
            _container = "Auto";
            _format = "Auto";
            _addMetadata = true;
            _downloadThumbnail = true;
            _downloadSubtitles = true;
            _downloadPlaylist = false;
            _useCustomPath = false;
            _downloadPath = "";
            Output = "";

            FreezeButton = false;
            DownloadButtonProgressIndeterminate = false;
            FormatsButtonProgressIndeterminate = false;
            DownloadButtonProgressPercentageValue = 0.0; // 99 for 99%
            DownloadButtonProgressPercentageString = "_Download";
            FileSizeString = "";
            DownloadSpeedString = "";
            DownloadETAString = "";

            outputSeparators = new string[]
            {
                "[download]",
                "of",
                "at",
                "ETA",
                " "
            };
            outputString = new();
            PrepareDlProcess();

            var canOpenDownloadFolder = this.WhenAnyValue(
                x => x.DownloadPath,
                path => Directory.Exists(path));
            var canStartDl = this.WhenAnyValue(
                x => x.Link,
                x => x.Container,
                x => x.Format,
                x => x.DlBinaryPath,
                x => x.FreezeButton,
                (link, container, format, dlBinaryPath, freezeButton) => !string.IsNullOrEmpty(link) && !string.IsNullOrEmpty(container) && !string.IsNullOrEmpty(format) && !string.IsNullOrEmpty(dlBinaryPath) && !freezeButton);
            var canAbortDl = this.WhenAnyValue(x => x.FreezeButton);

            BrowseDownloadFolderCommand = ReactiveCommand.Create(BrowseDownloadFolder);
            OpenDownloadFolderCommand = ReactiveCommand.Create(OpenDownloadFolder, canOpenDownloadFolder);
            StartDownloadCommand = ReactiveCommand.Create(StartDownload, canStartDl);
            ListFormatsCommand = ReactiveCommand.Create(ListFormats, canStartDl);
            AbortDlCommand = ReactiveCommand.Create(AbortDl, canAbortDl);

            ContainerList = new()
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

            FormatList = new()
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
                "YouTube 4K 60fps HDR AV1 + Opus WebM (701+251)",
                "YouTube 4K 60fps HDR VP9 + Opus WebM (337+251)",
                "YouTube 4K 60fps AV1 + Opus WebM (401+251)",
                "YouTube 4K 60fps VP9 + Opus WebM (315+251)",
                "YouTube 4K AV1 + Opus WebM (401+251)",
                "YouTube 4K VP9 + Opus WebM (313+251)",
                "YouTube 1440p60 HDR AV1 + Opus WebM (700+251)",
                "YouTube 1440p60 HDR VP9 + Opus WebM (336+251)",
                "YouTube 1440p60 AV1 + Opus WebM (400+251)",
                "YouTube 1440p60 VP9 + Opus WebM (308+251)",
                "YouTube 1440p AV1 + Opus WebM (400+251)",
                "YouTube 1440p VP9 + Opus WebM (271+251)",
                "YouTube 1080p60 AV1 + Opus WebM (399+251)",
                "YouTube 1080p60 VP9 + Opus WebM (303+251)",
                "YouTube 1080p60 AVC + AAC MP4 (299+140)",
                "YouTube 1080p AV1 + Opus WebM (399+251)",
                "YouTube 1080p VP9 + Opus WebM (248+251)",
                "YouTube 1080p AVC + AAC MP4 (137+140)",
                "YouTube 720p60 AV1 + Opus WebM (398+251)",
                "YouTube 720p60 VP9 + Opus WebM (302+251)",
                "YouTube 720p60 AVC + AAC MP4 (298+140)",
                "YouTube 720p AV1 + Opus WebM (398+251)",
                "YouTube 720p VP9 + Opus WebM (247+251)",
                "YouTube 720p AVC + AAC (136+140)",
                "1080p",
                "720p",
            };

            settingsFromHomeEvent = EventAggregator.Instance.GetEvent<SettingsFromHomeEvent>();
            // subscribe to settings changes from SettingsViewModel
            EventAggregator.Instance.GetEvent<SettingsChangedEvent>().Subscribe(x =>
            {
                _settings = x;
                ApplySettings();
            });
        }

        private Settings _settings;
        private bool _updated;
        private readonly SettingsFromHomeEvent settingsFromHomeEvent;

        private string _link;
        private string _container;
        private string _format;
        private bool _addMetadata;
        private bool _downloadThumbnail;
        private bool _downloadSubtitles;
        private bool _downloadPlaylist;
        private bool _useCustomPath;
        private string _downloadPath;

        private readonly string[] outputSeparators;
        private readonly StringBuilder outputString;
        private Process dlProcess;

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;

        public ReactiveCommand<Unit, Unit> BrowseDownloadFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenDownloadFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> StartDownloadCommand { get; }
        public ReactiveCommand<Unit, Unit> ListFormatsCommand { get; }
        public ReactiveCommand<Unit, Unit> AbortDlCommand { get; }

        /// <summary>
        /// Apply new settings published by SettingsViewModel.
        /// </summary>
        private void ApplySettings()
        {
            DlBinaryPath = _settings.DlPath;
            dlProcess.StartInfo.FileName = DlBinaryPath;
            this.RaiseAndSetIfChanged(ref _container, _settings.Container);
            this.RaiseAndSetIfChanged(ref _format, _settings.Format);
            this.RaiseAndSetIfChanged(ref _addMetadata, _settings.AddMetadata);
            this.RaiseAndSetIfChanged(ref _downloadThumbnail, _settings.DownloadThumbnail);
            this.RaiseAndSetIfChanged(ref _downloadSubtitles, _settings.DownloadSubtitles);
            this.RaiseAndSetIfChanged(ref _downloadPlaylist, _settings.DownloadPlaylist);
            this.RaiseAndSetIfChanged(ref _useCustomPath, _settings.UseCustomPath);
            this.RaiseAndSetIfChanged(ref _downloadPath, _settings.DownloadPath);

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (!_updated && !string.IsNullOrEmpty(_settings.DlPath) && _settings.AutoUpdateDl)
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
        [MemberNotNull(nameof(dlProcess))]
        private void PrepareDlProcess()
        {
            dlProcess = new();
            dlProcess.StartInfo.FileName = DlBinaryPath;
            dlProcess.StartInfo.CreateNoWindow = true;
            dlProcess.StartInfo.UseShellExecute = false;
            dlProcess.StartInfo.RedirectStandardError = true;
            dlProcess.StartInfo.RedirectStandardOutput = true;
            dlProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            dlProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            dlProcess.EnableRaisingEvents = true;
            dlProcess.ErrorDataReceived += DlOutputHandler;
            dlProcess.OutputDataReceived += DlOutputHandler;
            dlProcess.Exited += DlProcess_Exited;
        }

        private void DlProcess_Exited(object? sender, EventArgs e)
        {
            dlProcess.CancelErrorRead();
            dlProcess.CancelOutputRead();
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                FreezeButton = false;
                DownloadButtonProgressIndeterminate = false;
                FormatsButtonProgressIndeterminate = false;
                DownloadButtonProgressPercentageString = "_Download";
            });
        }

        private void BrowseDownloadFolder()
        {
            Microsoft.Win32.OpenFileDialog folderDialog = new()
            {
                FileName = "Folder Selection.",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                InitialDirectory = DownloadPath,
            };

            var result = folderDialog.ShowDialog();
            if (result == true)
            {
                DownloadPath = Path.GetDirectoryName(folderDialog.FileName) ?? "";
            }
        }

        private void OpenDownloadFolder()
        {
            try
            {
                WpfHelper.OpenLink(DownloadPath);
            }
            catch (Exception ex)
            {
                Output = ex.Message;
            }
        }

        private void StartDownload()
        {
            FreezeButton = true;
            DownloadButtonProgressIndeterminate = true;

            outputString.Clear();
            dlProcess.StartInfo.ArgumentList.Clear();

            try
            {
                // make parameter list
                if (!string.IsNullOrEmpty(_settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{_settings.Proxy}");
                }

                if (!string.IsNullOrEmpty(_settings.FfmpegPath))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--ffmpeg-location");
                    dlProcess.StartInfo.ArgumentList.Add($"{_settings.FfmpegPath}");
                }

                if (_format != "Auto") // custom format
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");

                    if (_format.Contains("YouTube "))
                    {
                        var parsedFormat = _format.Split(new char[] { '(', ')' });
                        if (parsedFormat.Length >= 2)
                            dlProcess.StartInfo.ArgumentList.Add($"{parsedFormat[1]}");
                        else
                            dlProcess.StartInfo.ArgumentList.Add($"{_format}");
                    }
                    else
                    {
                        dlProcess.StartInfo.ArgumentList.Add($"{_format}");
                    }

                    if (_container != "Auto") // merge into target container
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--merge-output-format");
                        dlProcess.StartInfo.ArgumentList.Add($"{_container}");
                    }
                }
                else if (_container != "Auto") // custom container
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{_container}");
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

        private void ListFormats()
        {
            FreezeButton = true;
            FormatsButtonProgressIndeterminate = true;

            outputString.Clear();
            dlProcess.StartInfo.ArgumentList.Clear();

            try
            {
                // make parameter list
                if (!string.IsNullOrEmpty(_settings.Proxy))
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

        private void AbortDl()
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

        private void UpdateDl()
        {
            FreezeButton = true;
            DownloadButtonProgressIndeterminate = true;
            FormatsButtonProgressIndeterminate = true;

            outputString.Clear();
            dlProcess.StartInfo.ArgumentList.Clear();

            try
            {
                // make parameter list
                if (!string.IsNullOrEmpty(_settings.Proxy))
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

        private void DlOutputHandler(object? sendingProcess, DataReceivedEventArgs outLine)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (!string.IsNullOrEmpty(outLine.Data))
                {
                    ParseDlOutput(outLine.Data);
                    outputString.Append(outLine.Data);
                    outputString.Append(Environment.NewLine);
                    Output = outputString.ToString();
                }
            });
        }

        private void ParseDlOutput(string output)
        {
            var parsedStringArray = output.Split(outputSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (parsedStringArray.Length == 4) // valid [download] line
            {
                var percentageString = parsedStringArray[0];
                if (percentageString.EndsWith('%')) // actual percentage
                {
                    // show percentage on button
                    DownloadButtonProgressPercentageString = percentageString;
                    // get percentage value for progress bar
                    var percentageNumberString = percentageString.TrimEnd('%');
                    if (double.TryParse(percentageNumberString, out var percentageNumber))
                    {
                        DownloadButtonProgressPercentageValue = percentageNumber;
                        DownloadButtonProgressIndeterminate = false;
                    }
                }
                // save other info
                FileSizeString = parsedStringArray[1];
                DownloadSpeedString = parsedStringArray[2];
                DownloadETAString = parsedStringArray[3];
            }
        }

        [Reactive]
        public string DlBinaryPath { get; set; }

        public string Link
        {
            get => _link;
            set
            {
                this.RaiseAndSetIfChanged(ref _link, value);
                if (string.IsNullOrEmpty(_settings.DlPath))
                    _snackbarMessageQueue.Enqueue("youtube-dl path is not set. Go to settings and set the path.");
            }
        }

        public ObservableCollection<string> ContainerList { get; }

        public string Container
        {
            get => _container;
            set
            {
                this.RaiseAndSetIfChanged(ref _container, value);
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
                this.RaiseAndSetIfChanged(ref _format, value);
                if (_format.Contains("AV1 + Opus WebM"))
                {
                    Container = "webm";
                }
                _settings.Format = _format;
                PublishSettings();
            }
        }

        public bool AddMetadata
        {
            get => _addMetadata;
            set
            {
                this.RaiseAndSetIfChanged(ref _addMetadata, value);
                _settings.AddMetadata = _addMetadata;
                PublishSettings();
            }
        }

        public bool DownloadThumbnail
        {
            get => _downloadThumbnail;
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadThumbnail, value);
                _settings.DownloadThumbnail = _downloadThumbnail;
                PublishSettings();
            }
        }

        public bool DownloadSubtitles
        {
            get => _downloadSubtitles;
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadSubtitles, value);
                _settings.DownloadSubtitles = _downloadSubtitles;
                PublishSettings();
            }
        }

        public bool DownloadPlaylist
        {
            get => _downloadPlaylist;
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadPlaylist, value);
                _settings.DownloadPlaylist = _downloadPlaylist;
                PublishSettings();
            }
        }

        public bool UseCustomPath
        {
            get => _useCustomPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _useCustomPath, value);
                _settings.UseCustomPath = _useCustomPath;
                PublishSettings();
            }
        }

        public string DownloadPath
        {
            get => _downloadPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadPath, value);
                _settings.DownloadPath = _downloadPath;
                PublishSettings();
            }
        }

        [Reactive]
        public string Output { get; set; }

        [Reactive]
        public bool FreezeButton { get; set; }

        [Reactive]
        public bool DownloadButtonProgressIndeterminate { get; set; }

        [Reactive]
        public bool FormatsButtonProgressIndeterminate { get; set; }

        [Reactive]
        public double DownloadButtonProgressPercentageValue { get; set; }

        [Reactive]
        public string DownloadButtonProgressPercentageString { get; set; }

        [Reactive]
        public string FileSizeString { get; set; }

        [Reactive]
        public string DownloadSpeedString { get; set; }

        [Reactive]
        public string DownloadETAString { get; set; }
    }

    /// <summary>
    /// Raised by HomeViewModel when settings are changed.
    /// </summary>
    public class SettingsFromHomeEvent : EventBase<Settings>
    {
    }
}
