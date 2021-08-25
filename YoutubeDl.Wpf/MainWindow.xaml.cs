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
            });
        }
    }
}
