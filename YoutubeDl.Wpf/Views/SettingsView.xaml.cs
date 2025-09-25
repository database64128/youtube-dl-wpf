using MaterialDesignThemes.Wpf;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsView
    {
        public SettingsView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                // Color mode
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.AppColorMode,
                    view => view.systemColorModeRadioButton.IsChecked,
                    colorMode => colorMode == BaseTheme.Inherit)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.AppColorMode,
                    view => view.lightColorModeRadioButton.IsChecked,
                    colorMode => colorMode == BaseTheme.Light)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.AppColorMode,
                    view => view.darkColorModeRadioButton.IsChecked,
                    colorMode => colorMode == BaseTheme.Dark)
                    .DisposeWith(disposables);

                // Window size
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.WindowSizeText,
                    view => view.windowSizeTextBlock.Text)
                    .DisposeWith(disposables);

                // Backend
                this.Bind(ViewModel,
                    viewModel => viewModel.SharedSettings.Backend,
                    view => view.ytdlBackendTypeRadioButton.IsChecked,
                    backend => backend == BackendTypes.Ytdl,
                    isChecked => isChecked == true ? BackendTypes.Ytdl : BackendTypes.Ytdlp)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.Backend,
                    view => view.ytdlpBackendTypeRadioButton.IsChecked,
                    backend => backend == BackendTypes.Ytdlp)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.SharedSettings.BackendPath,
                    view => view.dlPathTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.IsDlBinaryValid,
                    view => view.dlPathHintTextBlock.Visibility,
                    conversionHint: BooleanToVisibilityHint.Inverse)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.GlobalArguments,
                    view => view.argumentsItemsControl.ItemsSource)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.SharedSettings.BackendAutoUpdate,
                    view => view.autoUpdateDlToggle.IsChecked)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.BackendLastUpdateCheck,
                    view => view.lastUpdateCheckTextBlock.Text,
                    lastCheck =>
                    {
                        if (lastCheck == DateTimeOffset.MinValue)
                        {
                            return "Last check: never";
                        }
                        TimeSpan interval = DateTimeOffset.Now - lastCheck;
                        return interval.TotalSeconds switch
                        {
                            < 60 => "Last check: just now",
                            < 3600 => $"Last check: {interval.Minutes} minute(s) ago",
                            < 86400 => $"Last check: {interval.Hours} hour(s) ago",
                            _ => $"Last check: {interval.Days} day(s) ago",
                        };
                    })
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.SharedSettings.FfmpegPath,
                    view => view.ffmpegPathTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.IsFfmpegBinaryValid,
                    view => view.ffmpegPathHintTextBlock.Visibility,
                    conversionHint: BooleanToVisibilityHint.Inverse)
                    .DisposeWith(disposables);

                // Network
                this.Bind(ViewModel,
                    viewModel => viewModel.SharedSettings.Proxy,
                    view => view.proxyTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.SharedSettings.IsProxyUrlValid,
                    view => view.proxyHintTextBlock.Visibility,
                    conversionHint: BooleanToVisibilityHint.Inverse)
                    .DisposeWith(disposables);

                // Logging
                this.Bind(ViewModel,
                    viewModel => viewModel.SharedSettings.LoggingMaxEntries,
                    view => view.maxLogEntriesTextBox.Text)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.SharedSettings.LogToFiles,
                    view => view.logToFilesToggle.IsChecked)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.IsLogToFilesHintVisible,
                    view => view.logToFilesHintTextBlock.Visibility)
                    .DisposeWith(disposables);

                // About
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Version,
                    view => view.versionTextBlock.Text)
                    .DisposeWith(disposables);

                projectRepoHyperlink.Events().RequestNavigate
                                    .Select(args => args.Uri.AbsoluteUri)
                                    .Subscribe(WpfHelper.OpenUri)
                                    .DisposeWith(disposables);

                ytdlRepoHyperlink.Events().RequestNavigate
                                 .Select(args => args.Uri.AbsoluteUri)
                                 .Subscribe(WpfHelper.OpenUri)
                                 .DisposeWith(disposables);

                ytdlpRepoHyperlink.Events().RequestNavigate
                                  .Select(args => args.Uri.AbsoluteUri)
                                  .Subscribe(WpfHelper.OpenUri)
                                  .DisposeWith(disposables);

                // Color mode
                this.BindCommand(ViewModel,
                    viewModel => viewModel.ChangeColorModeCommand,
                    view => view.systemColorModeRadioButton,
                    Observable.Return(BaseTheme.Inherit))
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.ChangeColorModeCommand,
                    view => view.lightColorModeRadioButton,
                    Observable.Return(BaseTheme.Light))
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.ChangeColorModeCommand,
                    view => view.darkColorModeRadioButton,
                    Observable.Return(BaseTheme.Dark))
                    .DisposeWith(disposables);

                // Window size
                this.BindCommand(ViewModel,
                    viewModel => viewModel.ResetWindowSizeCommand,
                    view => view.resetWindowSizeButton)
                    .DisposeWith(disposables);

                // Browse buttons
                this.BindCommand(ViewModel,
                    viewModel => viewModel.SharedSettings.BrowseDlBinaryCommand,
                    view => view.dlPathBrowseButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.SharedSettings.BrowseFfmpegBinaryCommand,
                    view => view.ffmpegPathBrowseButton)
                    .DisposeWith(disposables);

                // Show in folder buttons
                this.BindCommand(ViewModel,
                    viewModel => viewModel.SharedSettings.ShowDlBinaryInFolderCommand,
                    view => view.dlPathShowInFolderButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.SharedSettings.ShowFfmpegBinaryInFolderCommand,
                    view => view.ffmpegPathShowInFolderButton)
                    .DisposeWith(disposables);

                // Check now button
                this.BindCommand(ViewModel,
                    viewModel => viewModel.BackendService.UpdateBackendCommand,
                    view => view.updateBackendButton)
                    .DisposeWith(disposables);

                // Log to files toggle
                this.BindCommand(ViewModel,
                    viewModel => viewModel.ToggleLogToFilesHintCommand,
                    view => view.logToFilesToggle)
                    .DisposeWith(disposables);
            });
        }
    }
}
