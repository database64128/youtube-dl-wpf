using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace YoutubeDl.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public const double MinWindowWidth = 672.0;

    public const double MinWindowHeight = 600.0;

    public MainWindow()
    {
        InitializeComponent();

        ViewModel = new();

        // Set window size here to avoid flickering.
        Width = ViewModel.SharedSettings.WindowWidth;
        Height = ViewModel.SharedSettings.WindowHeight;

        TaskbarItemInfo = new();

        this.WhenActivated(disposables =>
        {
            // Window size
            this.Bind(ViewModel,
                viewModel => viewModel.SharedSettings.WindowWidth,
                view => view.Width)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.SharedSettings.WindowHeight,
                view => view.Height)
                .DisposeWith(disposables);

            // Window closing
            this.Events().Closing
                .InvokeCommand(ViewModel.SaveSettingsCommand)
                .DisposeWith(disposables);

            // DialogHost
            this.Bind(ViewModel,
                viewModel => viewModel.IsDialogOpen,
                view => view.rootDialogHost.IsOpen)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                viewModel => viewModel.DialogVM,
                view => view.rootDialogHost.DialogContent)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                viewModel => viewModel.SnackbarMessageQueue,
                view => view.rootDialogHost.SnackbarMessageQueue)
                .DisposeWith(disposables);

            // Snackbar
            this.OneWayBind(ViewModel,
                viewModel => viewModel.SnackbarMessageQueue,
                view => view.mainSnackbar.MessageQueue)
                .DisposeWith(disposables);

            // Tabs
            this.OneWayBind(ViewModel,
                viewModel => viewModel.Tabs,
                view => view.mainTabControl.ItemsSource)
                .DisposeWith(disposables);

            // Taskbar progress
            this.OneWayBind(ViewModel,
                viewModel => viewModel.BackendService.GlobalDownloadProgressPercentage,
                view => view.TaskbarItemInfo.ProgressValue)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                viewModel => viewModel.BackendService.ProgressState,
                view => view.TaskbarItemInfo.ProgressState)
                .DisposeWith(disposables);
        });
    }
}
