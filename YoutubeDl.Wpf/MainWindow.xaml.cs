using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables;

namespace YoutubeDl.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            TaskbarItemInfo = new();
            ViewModel = new(MainSnackbar.MessageQueue!); // Null forgiving reason: following upstream
            MainSnackbar.MessageQueue!.DiscardDuplicates = true;
            this.WhenActivated(disposables =>
            {
                // Window closing
                this.Events().Closing
                    .InvokeCommand(ViewModel.SaveSettingsAsyncCommand)
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
}
