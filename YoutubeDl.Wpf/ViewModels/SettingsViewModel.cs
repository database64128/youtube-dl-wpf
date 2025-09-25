using DynamicData;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public partial class SettingsViewModel : ReactiveObject
{
    private readonly ISnackbarMessageQueue _snackbarMessageQueue;
    private readonly PaletteHelper _paletteHelper;

    public static PackIconKind TabItemHeaderIconKind { get; } = PackIconKind.Settings;

    public string Version { get; }

    public ObservableSettings SharedSettings { get; }

    public BackendService BackendService { get; }

    [ObservableAsProperty]
    private string _windowSizeText = "";

    [Reactive]
    private bool _isLogToFilesHintVisible;

    /// <summary>
    /// Gets the collection of view models of the arguments area.
    /// A view model in this collection must be of either
    /// <see cref="ArgumentChipViewModel"/> or <see cref="AddArgumentViewModel"/> type.
    /// </summary>
    public ObservableCollection<object> GlobalArguments { get; } = [];

    public SettingsViewModel(ObservableSettings settings, BackendService backendService, ISnackbarMessageQueue snackbarMessageQueue)
    {
        _snackbarMessageQueue = snackbarMessageQueue;
        _paletteHelper = new();

        Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "";
        SharedSettings = settings;
        BackendService = backendService;

        GlobalArguments.AddRange(SharedSettings.BackendGlobalArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)));
        GlobalArguments.Add(new AddArgumentViewModel(AddArgument));

        ChangeColorMode(SharedSettings.AppColorMode);

        this.WhenAnyValue(x => x.SharedSettings.LoggingMaxEntries)
            .Subscribe(loggingMaxEntries =>
            {
                if (loggingMaxEntries > 0)
                {
                    SharedSettings.AppSettings.LoggingMaxEntries = loggingMaxEntries;
                }
                else
                {
                    _snackbarMessageQueue.Enqueue("Warning: Max log entries must be positive.");
                    SharedSettings.LoggingMaxEntries = SharedSettings.AppSettings.LoggingMaxEntries;
                }
            });

        // Update window size text on size change.
        _windowSizeTextHelper = this
            .WhenAnyValue(x => x.SharedSettings.WindowWidth, x => x.SharedSettings.WindowHeight, GenerateWindowSizeText)
            .ToProperty(this, x => x.WindowSizeText);
    }

    private static string GenerateWindowSizeText(double width, double height) => $"{width:F} × {height:F}";

    [ReactiveCommand]
    private void ResetWindowSize()
    {
        SharedSettings.WindowWidth = Settings.DefaultWindowWidth;
        SharedSettings.WindowHeight = Settings.DefaultWindowHeight;
        SharedSettings.ConfigureDownloadRowDefinitionHeight = Settings.DefaultConfigureDownloadRowDefinitionHeight;
    }

    [ReactiveCommand]
    private void ChangeColorMode(BaseTheme colorMode)
    {
        // Get current theme.
        var theme = _paletteHelper.GetTheme();

        // Apply base theme
        theme.SetBaseTheme(colorMode);

        // Apply theme
        _paletteHelper.SetTheme(theme);

        // Save setting
        SharedSettings.AppColorMode = colorMode;
    }

    [ReactiveCommand]
    private void ToggleLogToFilesHint() => IsLogToFilesHintVisible = !IsLogToFilesHintVisible;

    private void DeleteArgumentChip(ArgumentChipViewModel item)
    {
        if (item.IsRemovable)
        {
            SharedSettings.BackendGlobalArguments.Remove(item.Argument);
            GlobalArguments.Remove(item);
        }
    }

    private void AddArgument(string argument)
    {
        var backendArgument = new BackendArgument(argument);
        SharedSettings.BackendGlobalArguments.Add(backendArgument);

        // Insert right before AddArgumentViewModel.
        GlobalArguments.Insert(GlobalArguments.Count - 1, new ArgumentChipViewModel(backendArgument, true, DeleteArgumentChip));
    }
}
