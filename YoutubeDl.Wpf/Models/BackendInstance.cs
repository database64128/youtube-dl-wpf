using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
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
using Windows.Win32;

namespace YoutubeDl.Wpf.Models;

public partial class BackendInstance : ReactiveObject, IEnableLogger
{
    private readonly ObservableSettings _settings;
    private readonly BackendService _backendService;
    private readonly Process _process;
    private readonly IObservable<bool> _canAbort;

    /// <summary>
    /// Gets the list of arguments to be passed to the backend process for all operations.
    /// </summary>
    public List<string> GenericArguments { get; } = [];

    /// <summary>
    /// Gets the list of arguments to be passed to the backend process for download operations.
    /// </summary>
    public List<string> GeneratedDownloadArguments { get; } = [];

    [Reactive]
    private double _downloadProgressPercentage; // 0.99 is 99%.

    [Reactive]
    private bool _statusIndeterminate;

    [Reactive]
    private bool _isRunning;

    [Reactive]
    private string _fileSizeString = "";

    [Reactive]
    private string _downloadSpeedString = "";

    [Reactive]
    private string _downloadETAString = "";

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

        _canAbort = this.WhenAnyValue(x => x.IsRunning);
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

    public void GenerateGenericArguments()
    {
        GenericArguments.Clear();

        if (!string.IsNullOrEmpty(_settings.Proxy))
        {
            GenericArguments.Add("--proxy");
            GenericArguments.Add(_settings.Proxy);
        }

        if (!string.IsNullOrEmpty(_settings.FfmpegPath))
        {
            GenericArguments.Add("--ffmpeg-location");
            GenericArguments.Add(_settings.FfmpegPath);
        }

        if (_settings.UseCookiesFile)
        {
            GenericArguments.Add("--cookies");
            GenericArguments.Add(_settings.CookiesFilePath);
        }

        if (_settings.UseCookiesBrowser)
        {
            GenericArguments.Add("--cookies-from-browser");
            GenericArguments.Add(_settings.CookiesBrowserArg);
        }
    }

    public void GenerateDownloadArguments(string playlistItems)
    {
        GeneratedDownloadArguments.Clear();

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
                GeneratedDownloadArguments.Add("--compat-options");
                GeneratedDownloadArguments.Add("no-keep-subs");
            }
        }

        if (_settings.DownloadSubtitles || _settings.DownloadAutoGeneratedSubtitles)
        {
            GeneratedDownloadArguments.Add("--embed-subs");

            if (!string.IsNullOrEmpty(_settings.SubtitleLanguages))
            {
                switch (_settings.Backend)
                {
                    case BackendTypes.Ytdl:
                        switch (_settings.SubtitleLanguages)
                        {
                            case "all":
                            case "all,-live_chat":
                                GeneratedDownloadArguments.Add("--all-subs");
                                break;
                            default:
                                GeneratedDownloadArguments.Add("--sub-lang");
                                GeneratedDownloadArguments.Add(_settings.SubtitleLanguages);
                                break;
                        }
                        break;

                    case BackendTypes.Ytdlp:
                        GeneratedDownloadArguments.Add("--sub-langs");
                        GeneratedDownloadArguments.Add(_settings.SubtitleLanguages);
                        break;
                }
            }
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

        if (_settings.UseCustomOutputTemplate)
        {
            GeneratedDownloadArguments.Add("-o");
            GeneratedDownloadArguments.Add(_settings.CustomOutputTemplate);
        }
    }

    private void PrepareProcessStartInfo()
    {
        _process.StartInfo.FileName = _settings.BackendPath;
        _process.StartInfo.ArgumentList.Clear();
        _process.StartInfo.ArgumentList.AddRange(_settings.BackendGlobalArguments.Select(x => x.Argument));
        _process.StartInfo.ArgumentList.AddRange(GenericArguments);
    }

    public async Task StartDownloadAsync(string link, CancellationToken cancellationToken = default)
    {
        PrepareProcessStartInfo();
        _process.StartInfo.ArgumentList.AddRange(GeneratedDownloadArguments);
        _process.StartInfo.ArgumentList.AddRange(_settings.AppSettings.BackendDownloadArguments.Select(x => x.Argument));
        _process.StartInfo.ArgumentList.Add(link);

        if (_settings.UseCustomPath)
        {
            _process.StartInfo.WorkingDirectory = _settings.DownloadPath;
        }

        try
        {
            await RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex, "Failed to start download.");
        }
    }

    public async Task ListFormatsAsync(string link, CancellationToken cancellationToken = default)
    {
        PrepareProcessStartInfo();
        _process.StartInfo.ArgumentList.Add("-F");
        _process.StartInfo.ArgumentList.Add(link);

        try
        {
            await RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex, "Failed to list formats.");
        }
    }

    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        _settings.BackendLastUpdateCheck = DateTimeOffset.Now;

        PrepareProcessStartInfo();
        _process.StartInfo.ArgumentList.Add("-U");

        try
        {
            await RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex, "Failed to update backend.");
        }
    }

    [ReactiveCommand(CanExecute = nameof(_canAbort))]
    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        if (PInvoke.AttachConsole((uint)_process.Id))
        {
            PInvoke.SetConsoleCtrlHandler(null, true);
            try
            {
                if (PInvoke.GenerateConsoleCtrlEvent(PInvoke.CTRL_C_EVENT, 0u))
                {
                    await _process.WaitForExitAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.Log().Error(ex, "Failed to abort process.");
            }
            finally
            {
                PInvoke.SetConsoleCtrlHandler(null, false);
                PInvoke.FreeConsole();
            }
        }
        this.Log().Info("🛑 Aborted.");
    }
}
