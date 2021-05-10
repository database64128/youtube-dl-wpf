using MaterialDesignThemes.Wpf;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows.Navigation;
using YoutubeDl.Wpf.Utils;
using YoutubeDl.Wpf.ViewModels;

namespace YoutubeDl.Wpf.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsView
    {
        public SettingsView(ISnackbarMessageQueue snackbarMessageQueue)
        {
            InitializeComponent();
            _snackbarMessageQueue = snackbarMessageQueue;
            ViewModel = new SettingsViewModel(_snackbarMessageQueue);
            this.WhenActivated(disposables =>
            {
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
                    view => view.lightColorModeRadioButton.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.AutoUpdateDl,
                    view => view.autoUpdateDlToggle.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.DlPath,
                    view => view.dlPathTextBox.Text)
                    .DisposeWith(disposables);
                this.Bind(ViewModel,
                    viewModel => viewModel.FfmpegPath,
                    view => view.ffmpegPathTextBox.Text)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Proxy,
                    view => view.proxyTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Version,
                    view => view.versionTextBlock.Text)
                    .DisposeWith(disposables);

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

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            WpfHelper.OpenLink(e.Uri.AbsoluteUri);
        }
    }
}
