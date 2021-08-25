using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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
                this.Bind(ViewModel,
                    viewModel => viewModel.FollowOSColorMode,
                    view => view.systemColorModeRadioButton.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.LightMode,
                    view => view.lightColorModeRadioButton.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.DarkMode,
                    view => view.darkColorModeRadioButton.IsChecked)
                    .DisposeWith(disposables);

                // Backend
                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.AutoUpdateDl,
                    view => view.autoUpdateDlToggle.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.DlPath,
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

                // Color mode
                this.BindCommand(ViewModel,
                    viewModel => viewModel.ChangeColorModeToSystem,
                    view => view.systemColorModeRadioButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.ChangeColorModeToLight,
                    view => view.lightColorModeRadioButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.ChangeColorModeToDark,
                    view => view.darkColorModeRadioButton)
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
