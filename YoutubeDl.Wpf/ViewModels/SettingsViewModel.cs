﻿using DynamicData;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels
{
    public partial class SettingsViewModel : ReactiveValidationObject
    {
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly PaletteHelper _paletteHelper;

        public static PackIconKind TabItemHeaderIconKind { get; } = PackIconKind.Settings;

        public string Version { get; }

        public ObservableSettings SharedSettings { get; }

        public BackendService BackendService { get; }

        [Reactive]
        private string _windowSizeText;

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
        }

        private static string GenerateWindowSizeText(double width, double height) => $"{width:F} × {height:F}";

        [ReactiveCommand]
        private void ResetWindowSize()
        {
            SharedSettings.WindowWidth = Settings.DefaultWindowWidth;
            SharedSettings.WindowHeight = Settings.DefaultWindowHeight;
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
}
