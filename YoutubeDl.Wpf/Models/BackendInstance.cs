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
    private readonly BackendService _backendService;
    private readonly Process _process;

    public List<string> GeneratedDownloadArguments { get; } = [];

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

        _process = new();
        _process.StartInfo.CreateNoWindow = true;
        _process.StartInfo.UseShellExecute = false;
        _process.StartInfo.RedirectStandardError = true;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        _process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        _process.EnableRaisingEvents = true;
    }

    private async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_process.Start())
            throw new InvalidOperationException("Method called when the backend process is running.");

        SetStatusRunning();

        await Task.WhenAll(
            ReadAndParseLinesAsync(_process.StandardError, cancellationToken),
            ReadAndParseLinesAsync(_process.StandardOutput, cancellationToken),
            _process.WaitForExitAsync(cancellationToken));

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

    private async Task ReadAndParseLinesAsync(StreamReader reader, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                return;

            this.Log().Info(line);
            ParseLine(line);
        }
    }

    private void ParseLine(ReadOnlySpan<char> line)
    {
        // Example lines:
        // [download]   0.0% of 36.35MiB at 20.40KiB/s ETA 30:24
        // [download]  65.1% of 36.35MiB at  2.81MiB/s ETA 00:04
        // [download] 100% of 36.35MiB in 00:10

        // Check and strip the download prefix.
        const string downloadPrefix = "[download] ";
        if (!line.StartsWith(downloadPrefix, StringComparison.Ordinal))
            return;
        line = line[downloadPrefix.Length..];

        // Parse and strip the percentage.
        const string percentageSuffix = "% of ";
        var percentageEnd = line.IndexOf(percentageSuffix, StringComparison.Ordinal);
        if (percentageEnd == -1 || !double.TryParse(line[..percentageEnd], NumberStyles.AllowLeadingWhite | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var percentage))
            return;
        DownloadProgressPercentage = percentage / 100;
        StatusIndeterminate = false;
        _backendService.UpdateProgress();
        line = line[(percentageEnd + percentageSuffix.Length)..];

        // Case 0: Download in progress
        const string speedPrefix = " at ";
        var sizeEnd = line.IndexOf(speedPrefix, StringComparison.Ordinal);
        if (sizeEnd != -1)
        {
            // Extract and strip file size.
            FileSizeString = line[..sizeEnd].ToString();
            line = line[(sizeEnd + speedPrefix.Length)..];

            // Extract and strip speed.
            const string etaPrefix = " ETA ";
            var speedEnd = line.IndexOf(etaPrefix, StringComparison.Ordinal);
            if (speedEnd == -1)
                return;
            DownloadSpeedString = line[..speedEnd].TrimStart().ToString();
            line = line[(speedEnd + etaPrefix.Length)..];

            // Extract ETA string.
            DownloadETAString = line.ToString();
            return;
        }

        // Case 1: Download finished
        sizeEnd = line.IndexOf(" in ", StringComparison.Ordinal);
        if (sizeEnd != -1)
        {
            // Extract file size.
            FileSizeString = line[..sizeEnd].ToString();
        }
    }

    public void GenerateDownloadArguments(string playlistItems)
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

            if (!string.IsNullOrEmpty(playlistItems))
            {
                GeneratedDownloadArguments.Add("--playlist-items");
                GeneratedDownloadArguments.Add(playlistItems);
            }
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
                _ => Settings.DefaultCustomOutputTemplate,
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
        _process.StartInfo.FileName = _settings.BackendPath;
        _process.StartInfo.ArgumentList.Clear();
        _process.StartInfo.ArgumentList.AddRange(_settings.BackendGlobalArguments.Select(x => x.Argument));
        _process.StartInfo.ArgumentList.AddRange(GeneratedDownloadArguments);
        _process.StartInfo.ArgumentList.AddRange(_settings.BackendDownloadArguments.Select(x => x.Argument));
        _process.StartInfo.ArgumentList.Add(link);

        try
        {
            await RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }

    public async Task ListFormatsAsync(string link, CancellationToken cancellationToken = default)
    {
        _process.StartInfo.FileName = _settings.BackendPath;
        _process.StartInfo.ArgumentList.Clear();
        _process.StartInfo.ArgumentList.AddRange(_settings.BackendGlobalArguments.Select(x => x.Argument));
        if (!string.IsNullOrEmpty(_settings.Proxy))
        {
            _process.StartInfo.ArgumentList.Add("--proxy");
            _process.StartInfo.ArgumentList.Add(_settings.Proxy);
        }
        _process.StartInfo.ArgumentList.Add("-F");
        _process.StartInfo.ArgumentList.Add(link);

        try
        {
            await RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }

    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        _settings.BackendLastUpdateCheck = DateTimeOffset.Now;

        _process.StartInfo.FileName = _settings.BackendPath;
        _process.StartInfo.ArgumentList.Clear();
        if (!string.IsNullOrEmpty(_settings.Proxy))
        {
            _process.StartInfo.ArgumentList.Add("--proxy");
            _process.StartInfo.ArgumentList.Add(_settings.Proxy);
        }
        _process.StartInfo.ArgumentList.Add("-U");

        try
        {
            await RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }

    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        if (CtrlCHelper.AttachConsole((uint)_process.Id))
        {
            CtrlCHelper.SetConsoleCtrlHandler(null, true);
            try
            {
                if (CtrlCHelper.GenerateConsoleCtrlEvent(CtrlCHelper.CTRL_C_EVENT, 0))
                {
                    await _process.WaitForExitAsync(cancellationToken);
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
}
