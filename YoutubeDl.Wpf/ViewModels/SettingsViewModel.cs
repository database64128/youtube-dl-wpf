﻿using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.ViewModels
{
    public class SettingsViewModel : ReactiveValidationObject
    {
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly PaletteHelper _paletteHelper;

        public string Version { get; }

        public Settings Settings { get; }

        [Reactive]
        public bool FollowOSColorMode { get; set; }

        [Reactive]
        public bool LightMode { get; set; }

        [Reactive]
        public bool DarkMode { get; set; }

        public ReactiveCommand<Unit, Unit> ChangeColorModeToSystem { get; }
        public ReactiveCommand<Unit, Unit> ChangeColorModeToLight { get; }
        public ReactiveCommand<Unit, Unit> ChangeColorModeToDark { get; }
        public ReactiveCommand<Unit, Unit> BrowseDlBinaryCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseFfmpegBinaryCommand { get; }
        public ReactiveCommand<string, Unit> OpenUri { get; }

        public SettingsViewModel(Settings settings, ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue;
            _paletteHelper = new();

            Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "";
            Settings = settings;

            switch (Settings.AppColorMode)
            {
                case BaseTheme.Inherit:
                    FollowOSColorMode = true;
                    LightMode = false;
                    DarkMode = false;
                    break;
                case BaseTheme.Light:
                    FollowOSColorMode = false;
                    LightMode = true;
                    DarkMode = false;
                    break;
                case BaseTheme.Dark:
                    FollowOSColorMode = false;
                    LightMode = false;
                    DarkMode = true;
                    break;
            }

            ChangeColorMode(Settings.AppColorMode);

            // The error messages won't be shown because INotifyDataErrorInfo only works with XAML bindings.
            // See https://github.com/reactiveui/ReactiveUI.Validation/issues/237.
            // These rules are kept here as a reference in case support gets added in a future version.
            this.ValidationRule(
                viewModel => viewModel.Settings.DlPath,
                dlPath => File.Exists(dlPath),
                "Invalid youtube-dl binary path.");

            this.ValidationRule(
                viewModel => viewModel.Settings.FfmpegPath,
                ffmpegPath => string.IsNullOrEmpty(ffmpegPath) || File.Exists(ffmpegPath),
                "Invalid ffmpeg binary path.");

            this.ValidationRule(
                viewModel => viewModel.Settings.Proxy,
                proxy => string.IsNullOrEmpty(proxy) || (Uri.TryCreate(proxy, UriKind.Absolute, out var uri) && (uri.Scheme is "socks5" or "http" or "https")),
                "Invalid proxy URL.");

            // The actual validation mechanisms.
            this.WhenAnyValue(x => x.Settings.DlPath)
                .Where(dlPath => !File.Exists(dlPath))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid youtube-dl binary path"));

            this.WhenAnyValue(x => x.Settings.FfmpegPath)
                .Where(ffmpegPath => !string.IsNullOrEmpty(ffmpegPath) && !File.Exists(ffmpegPath))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid ffmpeg binary path"));

            this.WhenAnyValue(x => x.Settings.Proxy)
                .Where(proxy => !string.IsNullOrEmpty(proxy) && !(Uri.TryCreate(proxy, UriKind.Absolute, out var uri) && (uri.Scheme is "socks5" or "http" or "https")))
                .Subscribe(_ => _snackbarMessageQueue.Enqueue("Warning: Invalid proxy URL"));

            ChangeColorModeToSystem = ReactiveCommand.Create(() => ChangeColorMode(BaseTheme.Inherit));
            ChangeColorModeToLight = ReactiveCommand.Create(() => ChangeColorMode(BaseTheme.Light));
            ChangeColorModeToDark = ReactiveCommand.Create(() => ChangeColorMode(BaseTheme.Dark));
            BrowseDlBinaryCommand = ReactiveCommand.Create(BrowseDlBinary);
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

        private void BrowseDlBinary() => Settings.DlPath = BrowseBinary("youtube-dl", Settings.DlPath);

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
    }
}
