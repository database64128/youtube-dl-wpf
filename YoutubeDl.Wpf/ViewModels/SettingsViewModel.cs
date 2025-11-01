using DynamicData;
using MaterialDesignColors.Recommended;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows.Media;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public partial class SettingsViewModel : ReactiveObject
{
    private readonly SnackbarMessageQueue _snackbarMessageQueue;
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
    public ObservableCollection<ReactiveObject> GlobalArguments { get; } = [];

    public SettingsViewModel(
        ObservableSettings settings,
        BackendService backendService,
        SnackbarMessageQueue snackbarMessageQueue,
        DateTime today)
    {
        _snackbarMessageQueue = snackbarMessageQueue;
        _paletteHelper = new();

        Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "";
        SharedSettings = settings;
        BackendService = backendService;

        GlobalArguments.AddRange(SharedSettings.BackendGlobalArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)));
        GlobalArguments.Add(new AddArgumentViewModel(AddArgument));

        // Theme colors easter egg
        (Color primary, Color secondary)? color = today.Month switch
        {
            10 => today.Day switch
            {
                31 => (OrangeSwatch.OrangeA700, OrangeSwatch.Orange500), // Halloween
                _ => null,
            },
            _ => null,
        };

        InitializeTheme(settings.AppColorMode, color);

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
    private void ChangeColorMode(BaseTheme baseTheme)
    {
        Theme theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(baseTheme);
        _paletteHelper.SetTheme(theme);

        SharedSettings.AppColorMode = baseTheme;
    }

    [ReactiveCommand]
    private void ToggleLogToFilesHint() => IsLogToFilesHintVisible = !IsLogToFilesHintVisible;

    private void InitializeTheme(BaseTheme baseTheme, (Color primary, Color secondary)? color)
    {
        if (baseTheme is BaseTheme.Inherit && color is null)
        {
            return;
        }

        Theme theme = _paletteHelper.GetTheme();
        if (baseTheme != BaseTheme.Inherit)
        {
            theme.SetBaseTheme(baseTheme);
        }
        if (color is not null)
        {
            theme.SetPrimaryColor(color.Value.primary);
            theme.SetSecondaryColor(color.Value.secondary);
        }
        _paletteHelper.SetTheme(theme);
    }

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
