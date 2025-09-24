using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
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
    public partial class MainWindowViewModel : ReactiveObject
    {
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly Settings _settings;

        public ObservableSettings SharedSettings { get; }
        public BackendService BackendService { get; }
        public object[] Tabs { get; }

        [Reactive]
        private object? _dialogVM;

        [Reactive]
        private bool _isDialogOpen;

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
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Sink(queuedTextBoxsink);
            if (_settings.LogToFiles)
            {
                loggerConfiguration = loggerConfiguration.WriteTo.File(".log", rollingInterval: RollingInterval.Day);
            }
            var logger = loggerConfiguration.CreateLogger();
            Locator.CurrentMutable.UseSerilogFullLogger(logger);

            SharedSettings = new(_settings);
            BackendService = new(SharedSettings);
            Tabs =
            [
                new HomeViewModel(SharedSettings, BackendService, queuedTextBoxsink, snackbarMessageQueue, OpenDialog, CloseDialog, CloseDialogCommand),
                new SettingsViewModel(SharedSettings, BackendService, snackbarMessageQueue),
            ];
        }

        private void OpenDialog(object? vm)
        {
            DialogVM = vm;
            IsDialogOpen = true;
        }

        [ReactiveCommand]
        private void CloseDialog() => IsDialogOpen = false;

        [ReactiveCommand]
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
