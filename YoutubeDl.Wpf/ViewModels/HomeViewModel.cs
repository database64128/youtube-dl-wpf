using DynamicData;
using DynamicData.Binding;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.ViewModels;

public partial class HomeViewModel : ReactiveObject
{
    private static readonly string[] s_predefinedCookiesBrowserArgs =
    [
        "brave", "chrome", "chromium", "edge", "firefox", "opera", "safari", "vivaldi", "whale",
    ];

    private static readonly string[] s_predefinedSubtitleLanguages = ["all", "all,-live_chat"];

    private readonly SnackbarMessageQueue _snackbarMessageQueue;
    private readonly PresetDialogViewModel _presetDialogVM;
    private readonly HashSet<string> _cookiesBrowserArgHistorySet;
    private readonly IObservable<bool> _canResetCustomOutputTemplate;
    private readonly IObservable<bool> _canBrowseDownloadFolder;
    private readonly IObservable<bool> _canOpenDownloadFolder;
    private readonly IObservable<bool> _canBrowseCookiesFile;
    private readonly IObservable<bool> _canShowCookiesFileInFolder;
    private readonly IObservable<bool> _canRun;
    private readonly IObservable<bool> _canEditOrDeletePreset;
    private readonly IObservable<bool> _canDuplicatePreset;
    private int _globalArgCount;
    private int _genericArgCount;
    private int _downloadArgCount;

    public PackIconKind TabItemHeaderIconKind { get; }

    public ObservableSettings SharedSettings { get; }

    public BackendService BackendService { get; }

    public BackendInstance BackendInstance { get; }

    public QueuedTextBoxSink QueuedTextBoxSink { get; }

    public ObservableCollection<Preset> Presets { get; } = [];

    /// <summary>
    /// Gets the collection of <c>--sub-langs</c> argument history.
    /// </summary>
    /// <remarks>
    /// This collection was first constructed from <see cref="Settings.SubtitleLanguagesHistory"/> in reverse order,
    /// so the newest argument is always the first element.
    /// </remarks>
    public ObservableCollection<HistoryItemViewModel> SubtitleLanguagesHistory { get; }

    /// <summary>
    /// Gets the output template history.
    /// This collection was first constructed from <see cref="Settings.OutputTemplateHistory"/> in reverse order.
    /// So the newest template is always the first element.
    /// </summary>
    public ObservableCollection<HistoryItemViewModel> OutputTemplateHistory { get; }

    /// <summary>
    /// Gets the download path history.
    /// This collection was first constructed from <see cref="Settings.DownloadPathHistory"/> in reverse order.
    /// So the newest path is always the first element.
    /// </summary>
    public ObservableCollection<HistoryItemViewModel> DownloadPathHistory { get; }

    /// <summary>
    /// Gets the cookies file path history.
    /// </summary>
    /// <remarks>
    /// This collection was first constructed from <see cref="Settings.CookiesFilePathHistory"/> in reverse order,
    /// so the newest path is always the first element.
    /// </remarks>
    public ObservableCollection<HistoryItemViewModel> CookiesFilePathHistory { get; }

    /// <summary>
    /// Gets the <c>--cookies-from-browser</c> argument history.
    /// </summary>
    /// <remarks>
    /// This collection was first constructed from <see cref="Settings.CookiesBrowserArgHistory"/> in reverse order,
    /// so the newest argument is always the first element.
    /// </remarks>
    public ObservableCollection<HistoryItemViewModel> CookiesBrowserArgHistory { get; }

    /// <summary>
    /// Gets the collection of view models of the arguments area.
    /// A view model in this collection must be of either
    /// <see cref="ArgumentChipViewModel"/> or <see cref="AddArgumentViewModel"/> type.
    /// </summary>
    public ObservableCollection<object> DownloadArguments { get; }

    [Reactive]
    private string _link = "";

    [Reactive]
    private string _playlistItems = "";

    public HomeViewModel(
        ObservableSettings settings,
        BackendService backendService,
        QueuedTextBoxSink queuedTextBoxSink,
        SnackbarMessageQueue snackbarMessageQueue,
        Action<object?> openDialog,
        Action closeDialog,
        ReactiveCommand<Unit, Unit> closeDialogCommand)
    {
        SharedSettings = settings;
        BackendService = backendService;
        BackendInstance = backendService.CreateInstance();
        QueuedTextBoxSink = queuedTextBoxSink;
        _snackbarMessageQueue = snackbarMessageQueue;
        _presetDialogVM = new(openDialog, closeDialog, closeDialogCommand);

        // Tab icon Easter egg.
        const PackIconKind defaultIcon = PackIconKind.Download;
        var today = DateTime.Today;
        TabItemHeaderIconKind = today.Month switch
        {
            2 => today.Day switch
            {
                14 => PackIconKind.Heart,
                _ => defaultIcon,
            },
            5 => today.Day switch
            {
                8 => PackIconKind.Cake,
                _ => defaultIcon,
            },
            10 => today.Day switch
            {
                31 => PackIconKind.Halloween,
                _ => defaultIcon,
            },
            11 => today.Day switch
            {
                >= 20 => PackIconKind.Thanksgiving,
                _ => defaultIcon,
            },
            12 => today.Day switch
            {
                >= 23 => PackIconKind.Snowman,
                _ => defaultIcon,
            },
            _ => defaultIcon,
        };

        this.WhenAnyValue(x => x.SharedSettings.Backend)
            .Subscribe(_ =>
            {
                Presets.Clear();
                Presets.AddRange(SharedSettings.AppSettings.CustomPresets.AsEnumerable().Reverse().Where(x => (x.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend));
                Presets.AddRange(Preset.PredefinedPresets.Where(x => (x.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend));
            });

        SubtitleLanguagesHistory =
        [
            .. SharedSettings.AppSettings.SubtitleLanguagesHistory.Select(x => new HistoryItemViewModel(x, DeleteSubtitleLanguagesItem)).Reverse(),
            .. s_predefinedSubtitleLanguages.Select(x => new HistoryItemViewModel(x, null)),
        ];
        OutputTemplateHistory = [.. SharedSettings.AppSettings.OutputTemplateHistory.Select(x => new HistoryItemViewModel(x, DeleteOutputTemplateItem)).Reverse()];
        DownloadPathHistory = [.. SharedSettings.AppSettings.DownloadPathHistory.Select(x => new HistoryItemViewModel(x, DeleteDownloadPathItem)).Reverse()];
        CookiesFilePathHistory = [.. SharedSettings.AppSettings.CookiesFilePathHistory.Select(x => new HistoryItemViewModel(x, DeleteCookiesFilePathItem)).Reverse()];
        CookiesBrowserArgHistory =
        [
            .. SharedSettings.AppSettings.CookiesBrowserArgHistory.Select(x => new HistoryItemViewModel(x, DeleteCookiesBrowserArgItem)).Reverse(),
            .. s_predefinedCookiesBrowserArgs.Select(x => new HistoryItemViewModel(x, null)),
        ];
        _cookiesBrowserArgHistorySet =
        [
            .. SharedSettings.AppSettings.CookiesBrowserArgHistory,
            .. s_predefinedCookiesBrowserArgs,
        ];
        DownloadArguments =
        [
            .. SharedSettings.AppSettings.BackendDownloadArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)),
            new AddArgumentViewModel(AddArgument),
        ];

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            _link = args[1];
        }

        SharedSettings.BackendGlobalArguments
            .ToObservableChangeSet()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => GenerateGlobalArguments());

        this.WhenAnyValue(
            x => x.SharedSettings.Proxy,
            x => x.SharedSettings.FfmpegPath,
            x => x.SharedSettings.UseCookiesFile,
            x => x.SharedSettings.CookiesFilePath,
            x => x.SharedSettings.UseCookiesBrowser,
            x => x.SharedSettings.CookiesBrowserArg,
            (_, _, _, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => GenerateGenericArguments());

        var genDownloadArgsObservable0 = this.WhenAnyValue(
            x => x.SharedSettings.DownloadSubtitles,
            x => x.SharedSettings.DownloadAutoGeneratedSubtitles,
            x => x.SharedSettings.SubtitleLanguages,
            x => x.SharedSettings.AddMetadata,
            x => x.SharedSettings.DownloadThumbnail,
            x => x.SharedSettings.DownloadPlaylist,
            x => x.PlaylistItems,
            (_, _, _, _, _, _, _) => Unit.Default);

        var genDownloadArgsObservable1 = this.WhenAnyValue(
            x => x.SharedSettings.UseCustomOutputTemplate,
            x => x.SharedSettings.CustomOutputTemplate,
            x => x.SharedSettings.UseCustomPath,
            x => x.SharedSettings.DownloadPath,
            x => x.SharedSettings.Backend,
            x => x.SharedSettings.SelectedPreset,
            (_, _, _, _, _, _) => Unit.Default);

        Observable.Merge(genDownloadArgsObservable0, genDownloadArgsObservable1)
                  .Throttle(TimeSpan.FromMilliseconds(250))
                  .ObserveOn(RxApp.MainThreadScheduler)
                  .Subscribe(_ => GenerateDownloadArguments());

        _canResetCustomOutputTemplate = this.WhenAnyValue(
            x => x.SharedSettings.UseCustomOutputTemplate,
            x => x.SharedSettings.CustomOutputTemplate,
            (useTemplate, template) => useTemplate && template != Settings.DefaultCustomOutputTemplate);

        _canBrowseDownloadFolder = this.WhenAnyValue(x => x.SharedSettings.UseCustomPath);

        _canOpenDownloadFolder = this.WhenAnyValue(
            x => x.SharedSettings.UseCustomPath,
            x => x.SharedSettings.DownloadPath,
            (useCustomPath, path) => useCustomPath && Directory.Exists(path));

        _canBrowseCookiesFile = this.WhenAnyValue(x => x.SharedSettings.UseCookiesFile);

        _canShowCookiesFileInFolder = this.WhenAnyValue(
            x => x.SharedSettings.UseCookiesFile,
            x => x.SharedSettings.CookiesFilePath,
            (useCookiesFile, path) => useCookiesFile && File.Exists(path));

        _canRun = this.WhenAnyValue(
            x => x.Link,
            x => x.BackendInstance.IsRunning,
            x => x.SharedSettings.IsDlBinaryValid,
            x => x.SharedSettings.IsFfmpegBinaryValid,
            (link, isRunning, isDlBinaryValid, isFfmpegBinaryValid) =>
                !string.IsNullOrEmpty(link) &&
                !isRunning &&
                isDlBinaryValid &&
                isFfmpegBinaryValid);

        _canEditOrDeletePreset = this.WhenAnyValue(
            x => x.SharedSettings.SelectedPreset,
            selectedPreset => selectedPreset is not null && !selectedPreset.IsPredefined);

        _canDuplicatePreset = this.WhenAnyValue(
            x => x.SharedSettings.SelectedPreset,
            (Preset? selectedPreset) => selectedPreset is not null);

        if (SharedSettings.IsDlBinaryValid)
        {
            if (SharedSettings.BackendAutoUpdate)
            {
                _ = BackendInstance.UpdateAsync();
            }
        }
        else
        {
            openDialog(new GetStartedDialogViewModel(SharedSettings, closeDialog));
        }
    }

    private void AddCustomPreset(Preset preset)
    {
        SharedSettings.AppSettings.CustomPresets.Add(preset);

        if ((preset.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend)
        {
            Presets.Insert(0, preset);
            SharedSettings.SelectedPreset = Presets.First();
        }
    }

    private void EditCustomPreset(Preset preset)
    {
        DeleteCustomPreset();
        AddCustomPreset(preset);
    }

    [ReactiveCommand]
    private void OpenAddCustomPresetDialog() => _presetDialogVM.AddOrEditPreset(Preset.Empty, AddCustomPreset);

    [ReactiveCommand(CanExecute = nameof(_canEditOrDeletePreset))]
    private void OpenEditCustomPresetDialog() => _presetDialogVM.AddOrEditPreset(SharedSettings.SelectedPreset!, EditCustomPreset);

    [ReactiveCommand(CanExecute = nameof(_canDuplicatePreset))]
    private void DuplicatePreset()
    {
        var dupNum = 0;
        string dupName;
        var preset = SharedSettings.SelectedPreset!;

        do
        {
            dupNum++;
            dupName = $"{preset.Name} ({dupNum})";
        }
        while (Presets.Any(x => x.Name == dupName));

        AddCustomPreset(preset.Duplicate(dupName));
    }

    [ReactiveCommand(CanExecute = nameof(_canEditOrDeletePreset))]
    private void DeleteCustomPreset()
    {
        var preset = SharedSettings.SelectedPreset!;
        _ = SharedSettings.AppSettings.CustomPresets.Remove(preset);
        _ = Presets.Remove(preset);
        SharedSettings.SelectedPreset = Presets.First();
    }

    private void DeleteSubtitleLanguagesItem(HistoryItemViewModel item)
    {
        _ = SharedSettings.AppSettings.SubtitleLanguagesHistory.Remove(item.Text);
        _ = SubtitleLanguagesHistory.Remove(item);
    }

    private void DeleteOutputTemplateItem(HistoryItemViewModel item)
    {
        _ = SharedSettings.AppSettings.OutputTemplateHistory.Remove(item.Text);
        _ = OutputTemplateHistory.Remove(item);
    }

    private void DeleteDownloadPathItem(HistoryItemViewModel item)
    {
        _ = SharedSettings.AppSettings.DownloadPathHistory.Remove(item.Text);
        _ = DownloadPathHistory.Remove(item);
    }

    private void DeleteCookiesFilePathItem(HistoryItemViewModel item)
    {
        _ = SharedSettings.AppSettings.CookiesFilePathHistory.Remove(item.Text);
        _ = CookiesFilePathHistory.Remove(item);
    }

    private void DeleteCookiesBrowserArgItem(HistoryItemViewModel item)
    {
        _ = _cookiesBrowserArgHistorySet.Remove(item.Text);
        _ = SharedSettings.AppSettings.CookiesBrowserArgHistory.Remove(item.Text);
        _ = CookiesBrowserArgHistory.Remove(item);
    }

    private void UpdateSubtitleLanguagesHistory()
    {
        if (!s_predefinedSubtitleLanguages.Contains(SharedSettings.SubtitleLanguages) &&
            !SharedSettings.AppSettings.SubtitleLanguagesHistory.Contains(SharedSettings.SubtitleLanguages))
        {
            SharedSettings.AppSettings.SubtitleLanguagesHistory.Add(SharedSettings.SubtitleLanguages);
            SubtitleLanguagesHistory.Insert(0, new(SharedSettings.SubtitleLanguages, DeleteSubtitleLanguagesItem));
        }
    }

    private void UpdateOutputTemplateHistory()
    {
        if (!SharedSettings.AppSettings.OutputTemplateHistory.Contains(SharedSettings.CustomOutputTemplate))
        {
            SharedSettings.AppSettings.OutputTemplateHistory.Add(SharedSettings.CustomOutputTemplate);
            OutputTemplateHistory.Insert(0, new(SharedSettings.CustomOutputTemplate, DeleteOutputTemplateItem));
        }
    }

    private void UpdateDownloadPathHistory()
    {
        if (!SharedSettings.AppSettings.DownloadPathHistory.Contains(SharedSettings.DownloadPath))
        {
            SharedSettings.AppSettings.DownloadPathHistory.Add(SharedSettings.DownloadPath);
            DownloadPathHistory.Insert(0, new(SharedSettings.DownloadPath, DeleteDownloadPathItem));
        }
    }

    private void UpdateCookiesFilePathHistory()
    {
        if (!SharedSettings.AppSettings.CookiesFilePathHistory.Contains(SharedSettings.CookiesFilePath))
        {
            SharedSettings.AppSettings.CookiesFilePathHistory.Add(SharedSettings.CookiesFilePath);
            CookiesFilePathHistory.Insert(0, new(SharedSettings.CookiesFilePath, DeleteCookiesFilePathItem));
        }
    }

    private void UpdateCookiesBrowserArgHistory()
    {
        if (!_cookiesBrowserArgHistorySet.Contains(SharedSettings.CookiesBrowserArg))
        {
            _cookiesBrowserArgHistorySet.Add(SharedSettings.CookiesBrowserArg);
            SharedSettings.AppSettings.CookiesBrowserArgHistory.Add(SharedSettings.CookiesBrowserArg);
            CookiesBrowserArgHistory.Insert(0, new(SharedSettings.CookiesBrowserArg, DeleteCookiesBrowserArgItem));
        }
    }

    private void DeleteArgumentChip(ArgumentChipViewModel item)
    {
        if (item.IsRemovable)
        {
            _ = SharedSettings.AppSettings.BackendDownloadArguments.Remove(item.Argument);
            _ = DownloadArguments.Remove(item);
        }
    }

    private void AddArgument(string argument)
    {
        var backendArgument = new BackendArgument(argument);
        SharedSettings.AppSettings.BackendDownloadArguments.Add(backendArgument);

        // Insert right before AddArgumentViewModel.
        DownloadArguments.Insert(DownloadArguments.Count - 1, new ArgumentChipViewModel(backendArgument, true, DeleteArgumentChip));
    }

    [ReactiveCommand(CanExecute = nameof(_canResetCustomOutputTemplate))]
    private void ResetCustomOutputTemplate() => SharedSettings.CustomOutputTemplate = Settings.DefaultCustomOutputTemplate;

    [ReactiveCommand(CanExecute = nameof(_canBrowseDownloadFolder))]
    private void BrowseDownloadFolder()
    {
        if (WpfHelper.BrowseFolder(SharedSettings.DownloadPath, out string? newPath))
        {
            SharedSettings.DownloadPath = newPath;
        }
    }

    [ReactiveCommand(CanExecute = nameof(_canOpenDownloadFolder))]
    private void OpenDownloadFolder()
    {
        try
        {
            WpfHelper.OpenUri(SharedSettings.DownloadPath);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex, "Failed to open the download folder.");
            _snackbarMessageQueue.Enqueue($"Failed to open the download folder: {ex.Message}");
        }
    }

    [ReactiveCommand(CanExecute = nameof(_canBrowseCookiesFile))]
    private void BrowseCookiesFile()
    {
        if (WpfHelper.BrowseFile(SharedSettings.CookiesFilePath, out string? newPath, "", ".txt", "Cookies Files|*.txt;*.cookies|All Files|*.*"))
        {
            SharedSettings.CookiesFilePath = newPath;
        }
    }

    [ReactiveCommand(CanExecute = nameof(_canShowCookiesFileInFolder))]
    private void ShowCookiesFileInFolder() => WpfHelper.ShowInFolder(SharedSettings.CookiesFilePath);

    private void GenerateGlobalArguments()
    {
        for (int i = 0; i < _globalArgCount; i++)
        {
            DownloadArguments.RemoveAt(0);
        }

        ObservableCollection<BackendArgument> globalArgs = SharedSettings.BackendGlobalArguments;
        _globalArgCount = globalArgs.Count;

        for (int i = 0; i < globalArgs.Count; i++)
        {
            DownloadArguments.Insert(i, new ArgumentChipViewModel(globalArgs[i], false, DeleteArgumentChip)); ;
        }
    }

    private void GenerateGenericArguments()
    {
        BackendInstance.GenerateGenericArguments();

        int index = _globalArgCount;

        for (int i = 0; i < _genericArgCount; i++)
        {
            DownloadArguments.RemoveAt(index);
        }

        List<string> genericArgs = BackendInstance.GenericArguments;
        _genericArgCount = genericArgs.Count;

        for (int i = 0; i < genericArgs.Count; i++)
        {
            DownloadArguments.Insert(index + i, new ArgumentChipViewModel(new(genericArgs[i]), false, DeleteArgumentChip));
        }
    }

    /// <summary>
    /// Generates arguments for a download operation and
    /// updates the corresponding argument chips.
    /// </summary>
    private void GenerateDownloadArguments()
    {
        BackendInstance.GenerateDownloadArguments(PlaylistItems);

        int index = _globalArgCount + _genericArgCount;

        for (int i = 0; i < _downloadArgCount; i++)
        {
            DownloadArguments.RemoveAt(index);
        }

        List<string> downloadArgs = BackendInstance.GeneratedDownloadArguments;
        _downloadArgCount = downloadArgs.Count;

        for (int i = 0; i < downloadArgs.Count; i++)
        {
            DownloadArguments.Insert(index + i, new ArgumentChipViewModel(new(downloadArgs[i]), false, DeleteArgumentChip));
        }
    }

    private bool ValidateGenericOptions()
    {
        if (SharedSettings.UseCookiesFile && !File.Exists(SharedSettings.CookiesFilePath))
        {
            _snackbarMessageQueue.Enqueue("The specified cookies file does not exist.");
            return false;
        }

        if (SharedSettings.UseCookiesBrowser && string.IsNullOrEmpty(SharedSettings.CookiesBrowserArg))
        {
            _snackbarMessageQueue.Enqueue("The browser argument for cookies is empty.");
            return false;
        }

        return true;
    }

    private bool ValidateDownloadOptions()
    {
        if (SharedSettings.UseCustomOutputTemplate && string.IsNullOrEmpty(SharedSettings.CustomOutputTemplate))
        {
            _snackbarMessageQueue.Enqueue("The custom output template is empty.");
            return false;
        }

        if (SharedSettings.UseCustomPath && !Directory.Exists(SharedSettings.DownloadPath))
        {
            _snackbarMessageQueue.Enqueue(
                "The specified download folder does not exist.", "Create",
                static (object? arg) =>
                {
                    if (arg is HomeViewModel vm)
                    {
                        try
                        {
                            _ = Directory.CreateDirectory(vm.SharedSettings.DownloadPath);
                            vm._snackbarMessageQueue.Enqueue("The download folder has been created.");
                        }
                        catch (Exception ex)
                        {
                            vm.Log().Error(ex, "Failed to create the download folder.");
                            vm._snackbarMessageQueue.Enqueue($"Failed to create the download folder: {ex.Message}");
                        }
                    }
                },
                this, false, false, TimeSpan.FromSeconds(10));
            return false;
        }

        return true;
    }

    private void UpdateGenericOptionsHistory()
    {
        if (SharedSettings.UseCookiesFile)
        {
            UpdateCookiesFilePathHistory();
        }

        if (SharedSettings.UseCookiesBrowser)
        {
            UpdateCookiesBrowserArgHistory();
        }
    }

    private void UpdateDownloadOptionsHistory()
    {
        if ((SharedSettings.DownloadSubtitles || SharedSettings.DownloadAutoGeneratedSubtitles) &&
            !string.IsNullOrEmpty(SharedSettings.SubtitleLanguages))
        {
            UpdateSubtitleLanguagesHistory();
        }

        if (SharedSettings.UseCustomOutputTemplate)
        {
            UpdateOutputTemplateHistory();
        }

        if (SharedSettings.UseCustomPath)
        {
            UpdateDownloadPathHistory();
        }
    }

    [ReactiveCommand(CanExecute = nameof(_canRun))]
    private Task StartDownloadAsync(string link, CancellationToken cancellationToken = default)
    {
        if (!ValidateGenericOptions() || !ValidateDownloadOptions())
        {
            return Task.CompletedTask;
        }

        UpdateGenericOptionsHistory();
        UpdateDownloadOptionsHistory();

        return BackendInstance.StartDownloadAsync(link, cancellationToken);
    }

    [ReactiveCommand(CanExecute = nameof(_canRun))]
    private Task ListFormatsAsync(string link, CancellationToken cancellationToken = default)
    {
        if (!ValidateGenericOptions())
        {
            return Task.CompletedTask;
        }

        UpdateGenericOptionsHistory();

        return BackendInstance.ListFormatsAsync(link, cancellationToken);
    }
}
