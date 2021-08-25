using MaterialDesignThemes.Wpf;
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
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.ViewModels
{
    public class HomeViewModel : ReactiveValidationObject
    {
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly string[] outputSeparators;
        private readonly StringBuilder outputString;
        private Process dlProcess;

        public Settings Settings { get; }

        public ObservableCollection<string> ContainerList { get; }

        public ObservableCollection<string> FormatList { get; }

        [Reactive]
        public string Link { get; set; } = "";

        [Reactive]
        public string Output { get; set; } = "";

        [Reactive]
        public bool FreezeButton { get; set; }

        [Reactive]
        public bool DownloadButtonProgressIndeterminate { get; set; }

        [Reactive]
        public bool FormatsButtonProgressIndeterminate { get; set; }

        [Reactive]
        public double DownloadButtonProgressPercentageValue { get; set; } // 99 for 99%

        [Reactive]
        public string DownloadButtonProgressPercentageString { get; set; } = "_Download";

        [Reactive]
        public string FileSizeString { get; set; } = "";

        [Reactive]
        public string DownloadSpeedString { get; set; } = "";

        [Reactive]
        public string DownloadETAString { get; set; } = "";

        public ReactiveCommand<Unit, Unit> BrowseDownloadFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenDownloadFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> StartDownloadCommand { get; }
        public ReactiveCommand<Unit, Unit> ListFormatsCommand { get; }
        public ReactiveCommand<Unit, Unit> AbortDlCommand { get; }

        public HomeViewModel(Settings settings, ISnackbarMessageQueue snackbarMessageQueue)
        {
            Settings = settings;
            _snackbarMessageQueue = snackbarMessageQueue;

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
                x => x.Settings.DownloadPath,
                path => Directory.Exists(path));

            var canStartDl = this.WhenAnyValue(
                x => x.Link,
                x => x.Settings.Container,
                x => x.Settings.Format,
                x => x.Settings.DlPath,
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

            if (Settings.AutoUpdateDl && !string.IsNullOrEmpty(Settings.DlPath))
            {
                UpdateDl();
            }
        }

        /// <summary>
        /// Initializes dlProcess with common settings.
        /// </summary>
        [MemberNotNull(nameof(dlProcess))]
        private void PrepareDlProcess()
        {
            dlProcess = new();
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
                InitialDirectory = Settings.DownloadPath,
            };

            var result = folderDialog.ShowDialog();
            if (result == true)
            {
                Settings.DownloadPath = Path.GetDirectoryName(folderDialog.FileName) ?? "";
            }
        }

        private void OpenDownloadFolder()
        {
            try
            {
                WpfHelper.OpenUri(Settings.DownloadPath);
            }
            catch (Exception ex)
            {
                _snackbarMessageQueue.Enqueue(ex.Message);
            }
        }

        private void StartDownload()
        {
            outputString.Clear();
            dlProcess.StartInfo.FileName = Settings.DlPath;
            dlProcess.StartInfo.ArgumentList.Clear();

            try
            {
                if (!string.IsNullOrEmpty(Settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.Proxy}");
                }

                if (!string.IsNullOrEmpty(Settings.FfmpegPath))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--ffmpeg-location");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.FfmpegPath}");
                }

                if (Settings.Format != "Auto") // custom format
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");

                    if (Settings.Format.Contains("YouTube "))
                    {
                        var parsedFormat = Settings.Format.Split(new char[] { '(', ')' });
                        if (parsedFormat.Length >= 2)
                            dlProcess.StartInfo.ArgumentList.Add($"{parsedFormat[1]}");
                        else
                            dlProcess.StartInfo.ArgumentList.Add($"{Settings.Format}");
                    }
                    else
                    {
                        dlProcess.StartInfo.ArgumentList.Add($"{Settings.Format}");
                    }

                    if (Settings.Container != "Auto") // merge into target container
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--merge-output-format");
                        dlProcess.StartInfo.ArgumentList.Add($"{Settings.Container}");
                    }
                    else if (Settings.Format.Contains("AV1 + Opus WebM"))
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--merge-output-format");
                        dlProcess.StartInfo.ArgumentList.Add("webm");
                    }
                }
                else if (Settings.Container != "Auto") // custom container
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.Container}");
                }

                if (Settings.AddMetadata)
                    dlProcess.StartInfo.ArgumentList.Add("--add-metadata");

                if (Settings.DownloadThumbnail)
                    dlProcess.StartInfo.ArgumentList.Add("--embed-thumbnail");

                if (Settings.DownloadSubtitles)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--write-sub");
                    dlProcess.StartInfo.ArgumentList.Add("--embed-subs");
                }

                if (Settings.DownloadPlaylist)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--yes-playlist");
                }
                else
                {
                    dlProcess.StartInfo.ArgumentList.Add("--no-playlist");
                }

                if (Settings.UseCustomPath)
                {
                    dlProcess.StartInfo.ArgumentList.Add("-o");
                    dlProcess.StartInfo.ArgumentList.Add($@"{Settings.DownloadPath}\%(title)s-%(id)s.%(ext)s");
                }

                dlProcess.StartInfo.ArgumentList.Add($"{Link}");

                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();

                FreezeButton = true;
                DownloadButtonProgressIndeterminate = true;
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
            outputString.Clear();
            dlProcess.StartInfo.FileName = Settings.DlPath;
            dlProcess.StartInfo.ArgumentList.Clear();

            try
            {
                if (!string.IsNullOrEmpty(Settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.Proxy}");
                }
                dlProcess.StartInfo.ArgumentList.Add($"-F");
                dlProcess.StartInfo.ArgumentList.Add($"{Link}");

                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();

                FreezeButton = true;
                FormatsButtonProgressIndeterminate = true;
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
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
            outputString.Clear();
            dlProcess.StartInfo.FileName = Settings.DlPath;
            dlProcess.StartInfo.ArgumentList.Clear();

            try
            {
                if (!string.IsNullOrEmpty(Settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.Proxy}");
                }
                dlProcess.StartInfo.ArgumentList.Add($"-U");

                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();

                FreezeButton = true;
                DownloadButtonProgressIndeterminate = true;
                FormatsButtonProgressIndeterminate = true;
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
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
    }
}
