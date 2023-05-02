using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.Models;

public class BackendInstance : ReactiveObject, IEnableLogger
{
    private readonly ObservableSettings _settings;
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

    public BackendInstance(ObservableSettings settings, BackendService backendService)
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
    }

    private async Task RunDlAsync(CancellationToken cancellationToken = default)
    {
        if (!_dlProcess.Start())
            throw new InvalidOperationException("Method called when the backend process is running.");

        SetStatusRunning();

        await Task.WhenAll(
            ReadAndParseAsync(_dlProcess.StandardError, cancellationToken),
            ReadAndParseAsync(_dlProcess.StandardOutput, cancellationToken),
            _dlProcess.WaitForExitAsync(cancellationToken));

        SetStatusStopped();
    }

    private void SetStatusRunning()
    {
        StatusIndeterminate = true;
        IsRunning = true;
        _backendService.UpdateProgress();
    }

    private void SetStatusStopped()
    {
        DownloadProgressPercentage = 0.0;
        StatusIndeterminate = false;
        IsRunning = false;
        _backendService.UpdateProgress();
    }

    private async Task ReadAndParseAsync(StreamReader reader, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                return;

            this.Log().Info(line);
            ParseDlOutput(line);
        }
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

        if (_settings.SelectedPreset is not null)
        {
            GeneratedDownloadArguments.AddRange(_settings.SelectedPreset.ToArgs());
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
            outputTemplate = $@"{_settings.DownloadPath}{Path.DirectorySeparatorChar}{outputTemplate}";
        }

        if (_settings.UseCustomOutputTemplate || _settings.UseCustomPath)
        {
            GeneratedDownloadArguments.Add("-o");
            GeneratedDownloadArguments.Add(outputTemplate);
        }
    }

    public async Task StartDownloadAsync(string link, CancellationToken cancellationToken = default)
    {
        _dlProcess.StartInfo.FileName = _settings.BackendPath;
        _dlProcess.StartInfo.ArgumentList.Clear();
        _dlProcess.StartInfo.ArgumentList.AddRange(_settings.BackendGlobalArguments.Select(x => x.Argument));
        _dlProcess.StartInfo.ArgumentList.AddRange(GeneratedDownloadArguments);
        _dlProcess.StartInfo.ArgumentList.AddRange(_settings.BackendDownloadArguments.Select(x => x.Argument));
        _dlProcess.StartInfo.ArgumentList.Add(link);

        try
        {
            await RunDlAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }

    public async Task ListFormatsAsync(string link, CancellationToken cancellationToken = default)
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
            await RunDlAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }

    public async Task AbortDlAsync(CancellationToken cancellationToken = default)
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

    public async Task UpdateDlAsync(CancellationToken cancellationToken = default)
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
            await RunDlAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }
}
