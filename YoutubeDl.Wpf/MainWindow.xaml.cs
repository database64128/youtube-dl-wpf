using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Shell;

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
                    viewModel => viewModel.GetHomeView,
                    view => view.dashboardTabItem.Content)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.GetSettingsView,
                    view => view.settingsTabItem.Content)
                    .DisposeWith(disposables);

                // Taskbar progress
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.GetHomeView.ViewModel!.DownloadButtonProgressPercentageValue, // Null forgiving reason: upstream limitation.
                    view => view.TaskbarItemInfo.ProgressValue,
                    percentage => percentage / 100.0);

                ViewModel.WhenAnyValue(
                    x => x.GetHomeView.ViewModel!.FormatsButtonProgressIndeterminate,
                    x => x.GetHomeView.ViewModel!.DownloadButtonProgressIndeterminate,
                    x => x.GetHomeView.ViewModel!.DownloadButtonProgressPercentageValue, // Null forgiving reason: upstream limitation.
                    (formatsIndeterminate, downloadIndeterminate, percentage) => (formatsIndeterminate || downloadIndeterminate, percentage))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(Observer.Create(((bool indeterminate, double percentage) x) =>
                    {
                        if (x.indeterminate)
                        {
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                        }
                        else if (x.percentage > 0.0)
                        {
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                        }
                        else
                        {
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                        }
                    }))
                    .DisposeWith(disposables);
            });
        }
    }
}
