using DynamicData;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

        public static PackIconKind TabItemHeaderIconKind => PackIconKind.Download;

        public Settings Settings { get; }

        public ObservableCollection<Format> Containers { get; } = new();

        public ObservableCollection<Format> Formats { get; } = new();

        /// <summary>
        /// Gets the download path history.
        /// This collection was first constructed from <see cref="Settings.DownloadPathHistory"/> in reverse order.
        /// So the newest path is always the first element.
        /// </summary>
        public ObservableCollection<DownloadPathItemViewModel> DownloadPathHistory { get; } = new();

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

        public ReactiveCommand<Unit, Unit> ResetCustomFilenameTemplateCommand { get; }
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

            this.WhenAnyValue(x => x.Settings.Backend)
                .Subscribe(backend =>
                {
                    Containers.Clear();
                    Containers.AddRange(Format.PredefinedContainers.Where(x => (x.SupportedBackends & Settings.Backend) == Settings.Backend));
                    Formats.Clear();
                    Formats.AddRange(Format.PredefinedFormats.Where(x => (x.SupportedBackends & Settings.Backend) == Settings.Backend));
                });

            DownloadPathHistory.AddRange(Settings.DownloadPathHistory.Select(x => new DownloadPathItemViewModel(x, DeleteDownloadPathItem)).Reverse());

            PrepareDlProcess();

            var canResetCustomFilenameTemplate = this.WhenAnyValue(
                x => x.Settings.UseCustomOutputTemplate,
                x => x.Settings.CustomOutputTemplate,
                (useTemplate, template) => useTemplate && template != Settings.DefaultCustomFilenameTemplate);

            var canBrowseDownloadFolder = this.WhenAnyValue(x => x.Settings.UseCustomPath);

            var canOpenDownloadFolder = this.WhenAnyValue(
                x => x.Settings.UseCustomPath,
                x => x.Settings.DownloadPath,
                (useCustomPath, path) => useCustomPath && Directory.Exists(path));

            var canStartDl = this.WhenAnyValue(
                x => x.Link,
                x => x.Settings.ContainerText,
                x => x.Settings.FormatText,
                x => x.Settings.UseCustomPath,
                x => x.Settings.DownloadPath,
                x => x.Settings.DlPath,
                x => x.FreezeButton,
                (link, container, format, useCustomPath, downloadPath, dlBinaryPath, freezeButton) => !string.IsNullOrEmpty(link) && !string.IsNullOrEmpty(container) && !string.IsNullOrEmpty(format) && (!useCustomPath || Directory.Exists(downloadPath)) && !string.IsNullOrEmpty(dlBinaryPath) && !freezeButton);

            var canAbortDl = this.WhenAnyValue(x => x.FreezeButton);

            ResetCustomFilenameTemplateCommand = ReactiveCommand.Create(ResetCustomFilenameTemplate, canResetCustomFilenameTemplate);
            BrowseDownloadFolderCommand = ReactiveCommand.Create(BrowseDownloadFolder, canBrowseDownloadFolder);
            OpenDownloadFolderCommand = ReactiveCommand.Create(OpenDownloadFolder, canOpenDownloadFolder);
            StartDownloadCommand = ReactiveCommand.Create(StartDownload, canStartDl);
            ListFormatsCommand = ReactiveCommand.Create(ListFormats, canStartDl);
            AbortDlCommand = ReactiveCommand.Create(AbortDl, canAbortDl);

            if (Settings.AutoUpdateDl && !string.IsNullOrEmpty(Settings.DlPath))
            {
                UpdateDl();
            }
        }

        private void DeleteDownloadPathItem(DownloadPathItemViewModel item)
        {
            Settings.DownloadPathHistory.Remove(item.Path);
            DownloadPathHistory.Remove(item);
        }

        private void UpdateDownloadPathHistory()
        {
            // No need to check if path is null or empty.
            // Because this code path can only be reached
            // when custom path is toggled and a valid path is supplied.
            if (!Settings.DownloadPathHistory.Contains(Settings.DownloadPath))
            {
                Settings.DownloadPathHistory.Add(Settings.DownloadPath);
                DownloadPathHistory.Insert(0, new(Settings.DownloadPath, DeleteDownloadPathItem));
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
                DownloadButtonProgressPercentageValue = 0.0;
                DownloadButtonProgressPercentageString = "_Download";
            });
        }

        private void ResetCustomFilenameTemplate()
        {
            Settings.CustomOutputTemplate = Settings.DefaultCustomFilenameTemplate;
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

                // Use '-f' if no specified format. With specified format, use '--merge-output-format'.
                var containerOption = "-f";

                if (Settings.SelectedFormat is null) // custom format
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.FormatText}");
                    containerOption = "--merge-output-format";
                }
                else if (Settings.SelectedFormat != Format.Auto) // Apply selected format
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.SelectedFormat.FormatArg}");
                    dlProcess.StartInfo.ArgumentList.AddRange(Settings.SelectedFormat.ExtraArgs);
                    containerOption = "--merge-output-format";
                }
                else if (Settings.SelectedContainer?.FormatArg is not null)
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.SelectedContainer.FormatArg}");
                    containerOption = "--merge-output-format";
                }

                if (Settings.SelectedContainer is null) // custom container
                {
                    dlProcess.StartInfo.ArgumentList.Add(containerOption);
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.ContainerText}");
                }
                else if (Settings.SelectedContainer != Format.Auto) // Apply selected container
                {
                    dlProcess.StartInfo.ArgumentList.Add(containerOption);
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.SelectedContainer.ContainerArg}");
                    dlProcess.StartInfo.ArgumentList.AddRange(Settings.SelectedContainer.ExtraArgs);
                }
                else if (Settings.SelectedFormat?.ContainerArg is not null)
                {
                    dlProcess.StartInfo.ArgumentList.Add(containerOption);
                    dlProcess.StartInfo.ArgumentList.Add($"{Settings.SelectedFormat.ContainerArg}");
                }

                if (Settings.DownloadSubtitles)
                {
                    if (Settings.Backend == BackendTypes.Ytdl)
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--write-sub");
                    }
                }

                if (Settings.DownloadSubtitlesAllLanguages)
                {
                    if (Settings.Backend == BackendTypes.Ytdl)
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--all-subs");
                    }

                    if (Settings.Backend == BackendTypes.Ytdlp)
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--sub-langs");
                        dlProcess.StartInfo.ArgumentList.Add("all");
                    }
                }

                if (Settings.DownloadAutoGeneratedSubtitles)
                {
                    if (Settings.Backend == BackendTypes.Ytdl)
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--write-auto-sub");
                    }

                    if (Settings.Backend == BackendTypes.Ytdlp)
                    {
                        dlProcess.StartInfo.ArgumentList.Add("--write-auto-subs");
                        // --embed-auto-subs pending https://github.com/yt-dlp/yt-dlp/issues/826
                    }
                }

                if (Settings.DownloadSubtitles || Settings.DownloadSubtitlesAllLanguages || Settings.DownloadAutoGeneratedSubtitles)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--embed-subs");
                }

                if (Settings.AddMetadata)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--add-metadata");
                }

                if (Settings.DownloadThumbnail)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--embed-thumbnail");
                }

                if (Settings.DownloadPlaylist)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--yes-playlist");
                }
                else
                {
                    dlProcess.StartInfo.ArgumentList.Add("--no-playlist");
                }

                var outputTemplate = Settings.UseCustomOutputTemplate switch
                {
                    true => Settings.CustomOutputTemplate,
                    false => Settings.Backend switch
                    {
                        BackendTypes.Ytdl => "%(title)s-%(id)s.%(ext)s",
                        _ => Settings.DefaultCustomFilenameTemplate,
                    },
                };

                if (Settings.UseCustomPath)
                {
                    outputTemplate = $@"{Settings.DownloadPath}\{outputTemplate}";
                    UpdateDownloadPathHistory();
                }

                if (Settings.UseCustomOutputTemplate || Settings.UseCustomPath)
                {
                    dlProcess.StartInfo.ArgumentList.Add("-o");
                    dlProcess.StartInfo.ArgumentList.Add(outputTemplate);
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
