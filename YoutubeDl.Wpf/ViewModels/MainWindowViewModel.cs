using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;
using Splat.Serilog;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly Settings _settings;
        private readonly ObservableSettings _observableSettings;
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;

        public BackendService BackendService { get; }
        public PresetDialogViewModel PresetDialogVM { get; }
        public HomeViewModel HomeVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public object[] Tabs { get; }

        [Reactive]
        public bool IsDialogOpen { get; set; }

        public ReactiveCommand<CancelEventArgs?, bool> SaveSettingsAsyncCommand { get; }

        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            var (settings, loadSettingsErrMsg) = Settings.LoadSettingsAsync().GetAwaiter().GetResult();
            if (loadSettingsErrMsg is not null)
            {
                snackbarMessageQueue.Enqueue(loadSettingsErrMsg);
            }

            _settings = settings;
            _observableSettings = new(settings);
            _snackbarMessageQueue = snackbarMessageQueue;

            // Configure logging.
            var queuedTextBoxsink = new QueuedTextBoxSink(_observableSettings);
            var logger = new LoggerConfiguration()
                .WriteTo.Sink(queuedTextBoxsink)
                .CreateLogger();
            Locator.CurrentMutable.UseSerilogFullLogger(logger);

            BackendService = new(_observableSettings);
            PresetDialogVM = new(ControlDialog);
            HomeVM = new(_observableSettings, BackendService, queuedTextBoxsink, PresetDialogVM, snackbarMessageQueue);
            SettingsVM = new(_observableSettings, BackendService, snackbarMessageQueue);
            Tabs = new object[]
            {
                HomeVM,
                SettingsVM,
            };

            SaveSettingsAsyncCommand = ReactiveCommand.CreateFromTask<CancelEventArgs?, bool>(SaveSettingsAsync);
        }

        public void ControlDialog(bool open) => IsDialogOpen = open;

        public async Task<bool> SaveSettingsAsync(CancelEventArgs? cancelEventArgs = null, CancellationToken cancellationToken = default)
        {
            _observableSettings.UpdateSettings(_settings);

            var errMsg = await Settings.SaveSettingsAsync(_settings, cancellationToken);
            if (errMsg is not null)
            {
                _snackbarMessageQueue.Enqueue(errMsg);

                // Cancel window closing
                if (cancelEventArgs is not null)
                    cancelEventArgs.Cancel = true;

                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
