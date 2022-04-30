using DynamicData;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.ObjectModel;
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

        public Settings Settings { get; }

        /// <summary>
        /// Gets the collection of view models of the arguments area.
        /// A view model in this collection must be of either
        /// <see cref="ArgumentChipViewModel"/> or <see cref="AddArgumentViewModel"/> type.
        /// </summary>
        public ObservableCollection<object> GlobalArguments { get; } = new();

        public ReactiveCommand<BaseTheme, Unit> ChangeColorModeCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDlBinaryCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateBackendCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseFfmpegBinaryCommand { get; }
        public ReactiveCommand<string, Unit> OpenUri { get; }

        public SettingsViewModel(Settings settings, BackendService backendService, ISnackbarMessageQueue snackbarMessageQueue)
        {
            _backendService = backendService;
            _snackbarMessageQueue = snackbarMessageQueue;
            _paletteHelper = new();

            Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "";
            Settings = settings;

            GlobalArguments.AddRange(Settings.BackendGlobalArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)));
            GlobalArguments.Add(new AddArgumentViewModel(AddArgument));

            ChangeColorMode(Settings.AppColorMode);

            // The error messages won't be shown because INotifyDataErrorInfo only works with XAML bindings.
            // See https://github.com/reactiveui/ReactiveUI.Validation/issues/237.
            // These rules are kept here as a reference in case support gets added in a future version.
            this.ValidationRule(
                viewModel => viewModel.Settings.BackendPath,
                dlPath => File.Exists(dlPath),
                "Invalid backend binary path.");

            this.ValidationRule(
                viewModel => viewModel.Settings.FfmpegPath,
                ffmpegPath => string.IsNullOrEmpty(ffmpegPath) || File.Exists(ffmpegPath),
                "Invalid ffmpeg binary path.");

            this.ValidationRule(
                viewModel => viewModel.Settings.Proxy,
                proxy => string.IsNullOrEmpty(proxy) || (Uri.TryCreate(proxy, UriKind.Absolute, out var uri) && (uri.Scheme is "socks5" or "http" or "https")),
                "Invalid proxy URL.");

            this.ValidationRule(
                viewModel => viewModel.Settings.LoggingMaxEntries,
                loggingMaxEntries => loggingMaxEntries > 0,
                "Max log entries must be greater than 0.");

            // The actual validation mechanisms.
            this.WhenAnyValue(x => x.Settings.BackendPath)
                .Where(dlPath => !File.Exists(dlPath))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid backend binary path"));

            this.WhenAnyValue(x => x.Settings.FfmpegPath)
                .Where(ffmpegPath => !string.IsNullOrEmpty(ffmpegPath) && !File.Exists(ffmpegPath))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid ffmpeg binary path"));

            this.WhenAnyValue(x => x.Settings.Proxy)
                .Where(proxy => !string.IsNullOrEmpty(proxy) && !(Uri.TryCreate(proxy, UriKind.Absolute, out var uri) && (uri.Scheme is "socks5" or "http" or "https")))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid proxy URL"));

            this.WhenAnyValue(x => x.Settings.LoggingMaxEntries)
                .Where(loggingMaxEntries => loggingMaxEntries <= 0)
                .Subscribe(_ =>
                {
                    _snackbarMessageQueue.Enqueue("Warning: Max log entries must be greater than 0.");
                    Settings.LoggingMaxEntries = 1024;
                });

            // Guess the backend type from binary name.
            this.WhenAnyValue(x => x.Settings.BackendPath)
                .Select(dlPath => Path.GetFileNameWithoutExtension(dlPath))
                .Subscribe(name =>
                {
                    Settings.Backend = name switch
                    {
                        "youtube-dl" => BackendTypes.Ytdl,
                        "yt-dlp" => BackendTypes.Ytdlp,
                        _ => Settings.Backend,
                    };
                });

            var canUpdateBackend = this.WhenAnyValue(x => x._backendService.CanUpdate);

            ChangeColorModeCommand = ReactiveCommand.Create<BaseTheme>(ChangeColorMode);
            BrowseDlBinaryCommand = ReactiveCommand.Create(BrowseDlBinary);
            UpdateBackendCommand = ReactiveCommand.Create(_backendService.UpdateBackend, canUpdateBackend);
            BrowseFfmpegBinaryCommand = ReactiveCommand.Create(BrowseFfmpegBinary);
            OpenUri = ReactiveCommand.Create<string>(uri => WpfHelper.OpenUri(uri));
        }

        private void ChangeColorMode(BaseTheme colorMode)
        {
            // Get current theme.
            var theme = _paletteHelper.GetTheme();

            // Get current system theme if required.
            var targetColorMode = colorMode switch
            {
                BaseTheme.Inherit => Theme.GetSystemTheme() ?? BaseTheme.Dark,
                _ => colorMode,
            };

            // Apply base theme
            switch (targetColorMode)
            {
                case BaseTheme.Light:
                    theme.SetBaseTheme(Theme.Light);
                    break;
                case BaseTheme.Dark:
                    theme.SetBaseTheme(Theme.Dark);
                    break;
            }

            // Apply theme
            _paletteHelper.SetTheme(theme);

            // Save setting
            Settings.AppColorMode = colorMode;
        }

        private void BrowseDlBinary() => Settings.BackendPath = BrowseBinary(Settings.Backend.ToExecutableName(), Settings.BackendPath);

        private void BrowseFfmpegBinary() => Settings.FfmpegPath = BrowseBinary("ffmpeg", Settings.FfmpegPath);

        private static string BrowseBinary(string filename, string path)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                FileName = filename,
                DefaultExt = ".exe",
                Filter = "Executables (.exe)|*.exe",
                InitialDirectory = Path.GetDirectoryName(path),
            };

            var result = openFileDialog.ShowDialog();
            return result == true ? openFileDialog.FileName : path;
        }

        private void DeleteArgumentChip(ArgumentChipViewModel item)
        {
            if (item.IsRemovable)
            {
                Settings.BackendGlobalArguments.Remove(item.Argument);
                GlobalArguments.Remove(item);
            }
        }

        private void AddArgument(string argument)
        {
            var backendArgument = new BackendArgument(argument);
            Settings.BackendGlobalArguments.Add(backendArgument);

            // Insert right before AddArgumentViewModel.
            GlobalArguments.Insert(GlobalArguments.Count - 1, new ArgumentChipViewModel(backendArgument, true, DeleteArgumentChip));
        }
    }
}
