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
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly Settings _settings;

        public ObservableSettings SharedSettings { get; }
        public BackendService BackendService { get; }
        public PresetDialogViewModel PresetDialogVM { get; }
        public object[] Tabs { get; }

        [Reactive]
        public bool IsDialogOpen { get; set; }

        public ReactiveCommand<CancelEventArgs?, bool> SaveSettingsAsyncCommand { get; }

        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue;

            try
            {
                _settings = Settings.LoadAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                snackbarMessageQueue.Enqueue(ex.Message);
                _settings = new();
            }

            // Configure logging.
            var queuedTextBoxsink = new QueuedTextBoxSink(_settings);
            var logger = new LoggerConfiguration()
                .WriteTo.Sink(queuedTextBoxsink)
                .CreateLogger();
            Locator.CurrentMutable.UseSerilogFullLogger(logger);

            SharedSettings = new(_settings);
            BackendService = new(SharedSettings);
            PresetDialogVM = new(ControlDialog);
            Tabs =
            [
                new HomeViewModel(SharedSettings, BackendService, queuedTextBoxsink, PresetDialogVM, snackbarMessageQueue),
                new SettingsViewModel(SharedSettings, BackendService, snackbarMessageQueue),
            ];

            SaveSettingsAsyncCommand = ReactiveCommand.CreateFromTask<CancelEventArgs?, bool>(SaveSettingsAsync);
        }

        private void ControlDialog(bool open) => IsDialogOpen = open;

        private async Task<bool> SaveSettingsAsync(CancelEventArgs? cancelEventArgs = null, CancellationToken cancellationToken = default)
        {
            SharedSettings.UpdateAppSettings();

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
