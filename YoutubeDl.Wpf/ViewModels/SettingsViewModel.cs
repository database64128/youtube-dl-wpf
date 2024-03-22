using DynamicData;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.ViewModels
{
    public class SettingsViewModel : ReactiveValidationObject
    {
        private readonly BackendService _backendService;
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly PaletteHelper _paletteHelper;

        public static PackIconKind TabItemHeaderIconKind { get; } = PackIconKind.Settings;

        public string Version { get; }

        public ObservableSettings SharedSettings { get; }

        [Reactive]
        public string WindowSizeText { get; set; }

        /// <summary>
        /// Gets the collection of view models of the arguments area.
        /// A view model in this collection must be of either
        /// <see cref="ArgumentChipViewModel"/> or <see cref="AddArgumentViewModel"/> type.
        /// </summary>
        public ObservableCollection<object> GlobalArguments { get; } = [];

        public ReactiveCommand<Unit, Unit> ResetWindowSizeCommand { get; }
        public ReactiveCommand<BaseTheme, Unit> ChangeColorModeCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDlBinaryCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateBackendCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseFfmpegBinaryCommand { get; }
        public ReactiveCommand<string, Unit> OpenUri { get; }

        public SettingsViewModel(ObservableSettings settings, BackendService backendService, ISnackbarMessageQueue snackbarMessageQueue)
        {
            _backendService = backendService;
            _snackbarMessageQueue = snackbarMessageQueue;
            _paletteHelper = new();

            Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "";
            SharedSettings = settings;
            WindowSizeText = GenerateWindowSizeText(settings.WindowWidth, settings.WindowHeight);

            GlobalArguments.AddRange(SharedSettings.BackendGlobalArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)));
            GlobalArguments.Add(new AddArgumentViewModel(AddArgument));

            ChangeColorMode(SharedSettings.AppColorMode);

            // The error messages won't be shown because INotifyDataErrorInfo only works with XAML bindings.
            // See https://github.com/reactiveui/ReactiveUI.Validation/issues/237.
            // These rules are kept here as a reference in case support gets added in a future version.
            this.ValidationRule(
                viewModel => viewModel.SharedSettings.BackendPath,
                dlPath => File.Exists(dlPath),
                "Invalid backend binary path.");

            this.ValidationRule(
                viewModel => viewModel.SharedSettings.FfmpegPath,
                ffmpegPath => string.IsNullOrEmpty(ffmpegPath) || File.Exists(ffmpegPath),
                "Invalid ffmpeg binary path.");

            this.ValidationRule(
                viewModel => viewModel.SharedSettings.Proxy,
                proxy => string.IsNullOrEmpty(proxy) || (Uri.TryCreate(proxy, UriKind.Absolute, out var uri) && (uri.Scheme is "socks5" or "http" or "https")),
                "Invalid proxy URL.");

            this.ValidationRule(
                viewModel => viewModel.SharedSettings.LoggingMaxEntries,
                loggingMaxEntries => loggingMaxEntries > 0,
                "Max log entries must be greater than 0.");

            // The actual validation mechanisms.
            this.WhenAnyValue(x => x.SharedSettings.BackendPath)
                .Where(dlPath => !File.Exists(dlPath))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid backend binary path"));

            this.WhenAnyValue(x => x.SharedSettings.FfmpegPath)
                .Where(ffmpegPath => !string.IsNullOrEmpty(ffmpegPath) && !File.Exists(ffmpegPath))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid ffmpeg binary path"));

            this.WhenAnyValue(x => x.SharedSettings.Proxy)
                .Where(proxy => !string.IsNullOrEmpty(proxy) && !(Uri.TryCreate(proxy, UriKind.Absolute, out var uri) && (uri.Scheme is "socks5" or "http" or "https")))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid proxy URL"));

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
            this.WhenAnyValue(x => x.SharedSettings.WindowWidth, x => x.SharedSettings.WindowHeight)
                .Subscribe(((double width, double height) x) => WindowSizeText = GenerateWindowSizeText(x.width, x.height));

            // Guess the backend type from binary name.
            this.WhenAnyValue(x => x.SharedSettings.BackendPath)
                .Select(dlPath => Path.GetFileNameWithoutExtension(dlPath))
                .Subscribe(name =>
                {
                    SharedSettings.Backend = name switch
                    {
                        "youtube-dl" => BackendTypes.Ytdl,
                        "yt-dlp" => BackendTypes.Ytdlp,
                        _ => SharedSettings.Backend,
                    };
                });

            var canUpdateBackend = this.WhenAnyValue(x => x._backendService.CanUpdate);

            ResetWindowSizeCommand = ReactiveCommand.Create(ResetWindowSize);
            ChangeColorModeCommand = ReactiveCommand.Create<BaseTheme>(ChangeColorMode);
            BrowseDlBinaryCommand = ReactiveCommand.Create(BrowseDlBinary);
            UpdateBackendCommand = ReactiveCommand.CreateFromTask(_backendService.UpdateBackendAsync, canUpdateBackend);
            BrowseFfmpegBinaryCommand = ReactiveCommand.Create(BrowseFfmpegBinary);
            OpenUri = ReactiveCommand.Create<string>(WpfHelper.OpenUri);
        }

        private static string GenerateWindowSizeText(double width, double height) => $"{width:F} × {height:F}";

        private void ResetWindowSize()
        {
            SharedSettings.WindowWidth = Settings.DefaultWindowWidth;
            SharedSettings.WindowHeight = Settings.DefaultWindowHeight;
        }

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

        private void BrowseDlBinary() => SharedSettings.BackendPath = BrowseBinary(SharedSettings.Backend.ToExecutableName(), SharedSettings.BackendPath);

        private void BrowseFfmpegBinary() => SharedSettings.FfmpegPath = BrowseBinary("ffmpeg", SharedSettings.FfmpegPath);

        private static string BrowseBinary(string filename, string path)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                FileName = filename,
                DefaultExt = ".exe",
                Filter = "Executables (.exe)|*.exe",
                InitialDirectory = Path.GetDirectoryName(path),
            };

            bool? result;
            try
            {
                result = openFileDialog.ShowDialog();
            }
            catch (Win32Exception)
            {
                // ShowDialog silently ignores InitialDirectory when the path points to a non-existent directory on an existing volume.
                // But it throws a System.ComponentModel.Win32Exception when the path points to a non-existent volume.
                // So we catch the exception and try again with an empty InitialDirectory.
                openFileDialog.InitialDirectory = "";
                result = openFileDialog.ShowDialog();
            }
            return result == true ? openFileDialog.FileName : path;
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
}
