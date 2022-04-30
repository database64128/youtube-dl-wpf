using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.Models;

public class BackendInstance : ReactiveObject, IEnableLogger
{
    private readonly Settings _settings;
    private readonly Process _dlProcess;
    private readonly BackendService _backendService;
    private readonly string[] outputSeparators =
    {
        "[download]",
        "of",
        "at",
        "ETA",
        "in",
        " ",
    };

    public List<string> GeneratedDownloadArguments { get; } = new();

    [Reactive]
    public double DownloadProgressPercentage { get; set; } // 0.99 is 99%.

    [Reactive]
    public bool StatusIndeterminate { get; set; }

    [Reactive]
    public bool IsRunning { get; set; }

    [Reactive]
    public string FileSizeString { get; set; } = "";

    [Reactive]
    public string DownloadSpeedString { get; set; } = "";

    [Reactive]
    public string DownloadETAString { get; set; } = "";

    public BackendInstance(Settings settings, BackendService backendService)
    {
        _settings = settings;
        _backendService = backendService;

        _dlProcess = new();
        _dlProcess.StartInfo.CreateNoWindow = true;
        _dlProcess.StartInfo.UseShellExecute = false;
        _dlProcess.StartInfo.RedirectStandardError = true;
        _dlProcess.StartInfo.RedirectStandardOutput = true;
        _dlProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        _dlProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        _dlProcess.EnableRaisingEvents = true;
        _dlProcess.ErrorDataReceived += DlOutputHandler;
        _dlProcess.OutputDataReceived += DlOutputHandler;
        _dlProcess.Exited += DlProcess_Exited;
    }

    private void DlOutputHandler(object? sendingProcess, DataReceivedEventArgs outLine)
    {
        this.Log().Info(outLine.Data);

        RxApp.MainThreadScheduler.Schedule(() =>
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                ParseDlOutput(outLine.Data);
            }
        });
    }

    private void ParseDlOutput(string output)
    {
        var parsedStringArray = output.Split(outputSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (parsedStringArray.Length >= 2) // valid [download] line
        {
            ReadOnlySpan<char> percentageString = parsedStringArray[0];
            if (percentageString.Length >= 2 && percentageString.EndsWith("%")) // actual percentage
            {
                if (double.TryParse(percentageString[..^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var percentageNumber))
                {
                    DownloadProgressPercentage = percentageNumber / 100;
                    StatusIndeterminate = false;
                    _backendService.UpdateProgress();
                }
            }

            // save other info
            FileSizeString = parsedStringArray[1];

            if (parsedStringArray.Length == 4)
            {
                DownloadSpeedString = parsedStringArray[2];
                DownloadETAString = parsedStringArray[3];
            }
        }
    }

    private void DlProcess_Exited(object? sender, EventArgs e)
    {
        _dlProcess.CancelErrorRead();
        _dlProcess.CancelOutputRead();

        RxApp.MainThreadScheduler.Schedule(() =>
        {
            DownloadProgressPercentage = 0.0;
            StatusIndeterminate = false;
            IsRunning = false;
            _backendService.UpdateProgress();
        });
    }

    public void GenerateDownloadArguments()
    {
        GeneratedDownloadArguments.Clear();

        if (!string.IsNullOrEmpty(_settings.Proxy))
        {
            GeneratedDownloadArguments.Add("--proxy");
            GeneratedDownloadArguments.Add(_settings.Proxy);
        }

        if (!string.IsNullOrEmpty(_settings.FfmpegPath))
        {
            GeneratedDownloadArguments.Add("--ffmpeg-location");
            GeneratedDownloadArguments.Add(_settings.FfmpegPath);
        }

        // Use '-f' if no specified format. With specified format, use '--merge-output-format'.
        var containerOption = "-f";

        if (_settings.SelectedFormat is null) // custom format
        {
            GeneratedDownloadArguments.Add("-f");
            GeneratedDownloadArguments.Add(_settings.FormatText);
            containerOption = "--merge-output-format";
        }
        else if (_settings.SelectedFormat != Format.Auto && _settings.SelectedFormat.FormatArg is not null) // Apply selected format
        {
            GeneratedDownloadArguments.Add("-f");
            GeneratedDownloadArguments.Add(_settings.SelectedFormat.FormatArg);
            GeneratedDownloadArguments.AddRange(_settings.SelectedFormat.ExtraArgs);
            containerOption = "--merge-output-format";
        }
        else if (_settings.SelectedContainer?.FormatArg is not null)
        {
            GeneratedDownloadArguments.Add("-f");
            GeneratedDownloadArguments.Add(_settings.SelectedContainer.FormatArg);
            containerOption = "--merge-output-format";
        }

        if (_settings.SelectedContainer is null) // custom container
        {
            GeneratedDownloadArguments.Add(containerOption);
            GeneratedDownloadArguments.Add(_settings.ContainerText);
        }
        else if (_settings.SelectedContainer != Format.Auto && _settings.SelectedContainer.ContainerArg is not null) // Apply selected container
        {
            GeneratedDownloadArguments.Add(containerOption);
            GeneratedDownloadArguments.Add(_settings.SelectedContainer.ContainerArg);
            GeneratedDownloadArguments.AddRange(_settings.SelectedContainer.ExtraArgs);
        }
        else if (_settings.SelectedFormat?.ContainerArg is not null)
        {
            GeneratedDownloadArguments.Add(containerOption);
            GeneratedDownloadArguments.Add(_settings.SelectedFormat.ContainerArg);
        }

        if (_settings.DownloadSubtitles)
        {
            if (_settings.Backend == BackendTypes.Ytdl)
            {
                GeneratedDownloadArguments.Add("--write-sub");
            }
        }

        if (_settings.DownloadSubtitlesAllLanguages)
        {
            if (_settings.Backend == BackendTypes.Ytdl)
            {
                GeneratedDownloadArguments.Add("--all-subs");
            }

            if (_settings.Backend == BackendTypes.Ytdlp)
            {
                GeneratedDownloadArguments.Add("--sub-langs");
                GeneratedDownloadArguments.Add("all");
            }
        }

        if (_settings.DownloadAutoGeneratedSubtitles)
        {
            if (_settings.Backend == BackendTypes.Ytdl)
            {
                GeneratedDownloadArguments.Add("--write-auto-sub");
            }

            if (_settings.Backend == BackendTypes.Ytdlp)
            {
                GeneratedDownloadArguments.Add("--write-auto-subs");
                // --embed-auto-subs pending https://github.com/yt-dlp/yt-dlp/issues/826
            }
        }

        if (_settings.DownloadSubtitles || _settings.DownloadSubtitlesAllLanguages || _settings.DownloadAutoGeneratedSubtitles)
        {
            GeneratedDownloadArguments.Add("--embed-subs");
        }

        if (_settings.AddMetadata)
        {
            if (_settings.Backend == BackendTypes.Ytdl)
            {
                GeneratedDownloadArguments.Add("--add-metadata");
            }

            if (_settings.Backend == BackendTypes.Ytdlp)
            {
                GeneratedDownloadArguments.Add("--embed-metadata");
            }
        }

        if (_settings.DownloadThumbnail)
        {
            GeneratedDownloadArguments.Add("--embed-thumbnail");
        }

        if (_settings.DownloadPlaylist)
        {
            GeneratedDownloadArguments.Add("--yes-playlist");
        }
        else
        {
            GeneratedDownloadArguments.Add("--no-playlist");
        }

        var outputTemplate = _settings.UseCustomOutputTemplate switch
        {
            true => _settings.CustomOutputTemplate,
            false => _settings.Backend switch
            {
                BackendTypes.Ytdl => "%(title)s-%(id)s.%(ext)s",
                _ => Settings.DefaultCustomFilenameTemplate,
            },
        };

        if (_settings.UseCustomPath)
        {
            outputTemplate = $@"{_settings.DownloadPath}\{outputTemplate}";
        }

        if (_settings.UseCustomOutputTemplate || _settings.UseCustomPath)
        {
            GeneratedDownloadArguments.Add("-o");
            GeneratedDownloadArguments.Add(outputTemplate);
        }
    }

    public void StartDownload(string link)
    {
        _dlProcess.StartInfo.FileName = _settings.BackendPath;
        _dlProcess.StartInfo.ArgumentList.Clear();
        _dlProcess.StartInfo.ArgumentList.AddRange(_settings.BackendGlobalArguments.Select(x => x.Argument));
        _dlProcess.StartInfo.ArgumentList.AddRange(GeneratedDownloadArguments);
        _dlProcess.StartInfo.ArgumentList.AddRange(_settings.BackendDownloadArguments.Select(x => x.Argument));
        _dlProcess.StartInfo.ArgumentList.Add(link);

        try
        {
            _dlProcess.Start();
            _dlProcess.BeginErrorReadLine();
            _dlProcess.BeginOutputReadLine();

            StatusIndeterminate = true;
            IsRunning = true;
            _backendService.UpdateProgress();
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }

    public void ListFormats(string link)
    {
        _dlProcess.StartInfo.FileName = _settings.BackendPath;
        _dlProcess.StartInfo.ArgumentList.Clear();
        _dlProcess.StartInfo.ArgumentList.AddRange(_settings.BackendGlobalArguments.Select(x => x.Argument));
        if (!string.IsNullOrEmpty(_settings.Proxy))
        {
            _dlProcess.StartInfo.ArgumentList.Add("--proxy");
            _dlProcess.StartInfo.ArgumentList.Add(_settings.Proxy);
        }
        _dlProcess.StartInfo.ArgumentList.Add("-F");
        _dlProcess.StartInfo.ArgumentList.Add(link);

        try
        {
            _dlProcess.Start();
            _dlProcess.BeginErrorReadLine();
            _dlProcess.BeginOutputReadLine();

            StatusIndeterminate = true;
            IsRunning = true;
            _backendService.UpdateProgress();
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }

    public async Task AbortDl(CancellationToken cancellationToken = default)
    {
        if (CtrlCHelper.AttachConsole((uint)_dlProcess.Id))
        {
            CtrlCHelper.SetConsoleCtrlHandler(null, true);
            try
            {
                if (CtrlCHelper.GenerateConsoleCtrlEvent(CtrlCHelper.CTRL_C_EVENT, 0))
                {
                    await _dlProcess.WaitForExitAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.Log().Error(ex);
            }
            finally
            {
                CtrlCHelper.SetConsoleCtrlHandler(null, false);
                CtrlCHelper.FreeConsole();
            }
        }
        this.Log().Info("🛑 Aborted.");
    }

    public void UpdateDl()
    {
        _settings.BackendLastUpdateCheck = DateTimeOffset.Now;

        _dlProcess.StartInfo.FileName = _settings.BackendPath;
        _dlProcess.StartInfo.ArgumentList.Clear();
        if (!string.IsNullOrEmpty(_settings.Proxy))
        {
            _dlProcess.StartInfo.ArgumentList.Add("--proxy");
            _dlProcess.StartInfo.ArgumentList.Add(_settings.Proxy);
        }
        _dlProcess.StartInfo.ArgumentList.Add("-U");

        try
        {
            _dlProcess.Start();
            _dlProcess.BeginErrorReadLine();
            _dlProcess.BeginOutputReadLine();

            StatusIndeterminate = true;
            IsRunning = true;
            _backendService.UpdateProgress();
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }
}
