using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;
using Splat.Serilog;
using System;
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
            try
            {
                _settings = Settings.LoadAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                snackbarMessageQueue.Enqueue(ex.Message);
                _settings = new();
            }

            _observableSettings = new(_settings);
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

            try
            {
                await _settings.SaveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _snackbarMessageQueue.Enqueue(ex.Message);

                // Cancel window closing
                if (cancelEventArgs is not null)
                    cancelEventArgs.Cancel = true;

                return false;
            }

            return true;
        }
    }
}
