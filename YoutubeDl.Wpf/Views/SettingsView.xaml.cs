using MaterialDesignThemes.Wpf;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using YoutubeDl.Wpf.Models;

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
                    viewModel => viewModel.Settings.AppColorMode,
                    view => view.systemColorModeRadioButton.IsChecked,
                    colorMode => colorMode == BaseTheme.Inherit)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Settings.AppColorMode,
                    view => view.lightColorModeRadioButton.IsChecked,
                    colorMode => colorMode == BaseTheme.Light)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Settings.AppColorMode,
                    view => view.darkColorModeRadioButton.IsChecked,
                    colorMode => colorMode == BaseTheme.Dark)
                    .DisposeWith(disposables);

                // Backend
                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.Backend,
                    view => view.ytdlBackendTypeRadioButton.IsChecked,
                    backend => backend == BackendTypes.Ytdl,
                    isChecked => isChecked == true ? BackendTypes.Ytdl : BackendTypes.Ytdlp)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Settings.Backend,
                    view => view.ytdlpBackendTypeRadioButton.IsChecked,
                    backend => backend == BackendTypes.Ytdlp)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.BackendAutoUpdate,
                    view => view.autoUpdateDlToggle.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.BackendPath,
                    view => view.dlPathTextBox.Text)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.FfmpegPath,
                    view => view.ffmpegPathTextBox.Text)
                    .DisposeWith(disposables);

                // Network
                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.Proxy,
                    view => view.proxyTextBox.Text)
                    .DisposeWith(disposables);

                // About
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Version,
                    view => view.versionTextBlock.Text)
                    .DisposeWith(disposables);

                projectRepoHyperlink.Events().RequestNavigate
                                    .Select(args => args.Uri.AbsoluteUri)
                                    .InvokeCommand(ViewModel!.OpenUri) // Null forgiving reason: upstream limitation.
                                    .DisposeWith(disposables);

                ytdlRepoHyperlink.Events().RequestNavigate
                                 .Select(args => args.Uri.AbsoluteUri)
                                 .InvokeCommand(ViewModel.OpenUri)
                                 .DisposeWith(disposables);

                ytdlpRepoHyperlink.Events().RequestNavigate
                                  .Select(args => args.Uri.AbsoluteUri)
                                  .InvokeCommand(ViewModel.OpenUri)
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

                // Browse buttons
                this.BindCommand(ViewModel,
                    viewModel => viewModel.BrowseDlBinaryCommand,
                    view => view.dlPathBrowseButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.BrowseFfmpegBinaryCommand,
                    view => view.ffmpegPathBrowseButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
