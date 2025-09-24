using DynamicData;
using DynamicData.Binding;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Models;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.ViewModels
{
    public partial class HomeViewModel : ReactiveObject
    {
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly PresetDialogViewModel _presetDialogVM;
        private readonly IObservable<bool> _canResetCustomOutputTemplate;
        private readonly IObservable<bool> _canBrowseDownloadFolder;
        private readonly IObservable<bool> _canOpenDownloadFolder;
        private readonly IObservable<bool> _canRun;
        private readonly IObservable<bool> _canEditOrDeletePreset;
        private readonly IObservable<bool> _canDuplicatePreset;
        private int _globalArgCount;

        public PackIconKind TabItemHeaderIconKind { get; }

        public ObservableSettings SharedSettings { get; }

        public BackendService BackendService { get; }

        public BackendInstance BackendInstance { get; }

        public QueuedTextBoxSink QueuedTextBoxSink { get; }

        public ObservableCollection<Preset> Presets { get; } = [];

        /// <summary>
        /// Gets the output template history.
        /// This collection was first constructed from <see cref="ObservableSettings.OutputTemplateHistory"/> in reverse order.
        /// So the newest template is always the first element.
        /// </summary>
        public ObservableCollection<HistoryItemViewModel> OutputTemplateHistory { get; }

        /// <summary>
        /// Gets the download path history.
        /// This collection was first constructed from <see cref="ObservableSettings.DownloadPathHistory"/> in reverse order.
        /// So the newest path is always the first element.
        /// </summary>
        public ObservableCollection<HistoryItemViewModel> DownloadPathHistory { get; }

        /// <summary>
        /// Gets the collection of view models of the arguments area.
        /// A view model in this collection must be of either
        /// <see cref="ArgumentChipViewModel"/> or <see cref="AddArgumentViewModel"/> type.
        /// </summary>
        public ObservableCollection<object> DownloadArguments { get; }

        [Reactive]
        private string _link = "";

        [Reactive]
        private string _playlistItems = "";

        public HomeViewModel(
            ObservableSettings settings,
            BackendService backendService,
            QueuedTextBoxSink queuedTextBoxSink,
            ISnackbarMessageQueue snackbarMessageQueue,
            Action<object?> openDialog,
            Action closeDialog,
            ReactiveCommand<Unit, Unit> closeDialogCommand)
        {
            SharedSettings = settings;
            BackendService = backendService;
            BackendInstance = backendService.CreateInstance();
            QueuedTextBoxSink = queuedTextBoxSink;
            _snackbarMessageQueue = snackbarMessageQueue;
            _presetDialogVM = new(openDialog, closeDialog, closeDialogCommand);

            // Tab icon Easter egg.
            const PackIconKind defaultIcon = PackIconKind.Download;
            var today = DateTime.Today;
            TabItemHeaderIconKind = today.Month switch
            {
                2 => today.Day switch
                {
                    14 => PackIconKind.Heart,
                    _ => defaultIcon,
                },
                5 => today.Day switch
                {
                    8 => PackIconKind.Cake,
                    _ => defaultIcon,
                },
                10 => today.Day switch
                {
                    31 => PackIconKind.Halloween,
                    _ => defaultIcon,
                },
                11 => today.Day switch
                {
                    >= 20 => PackIconKind.Thanksgiving,
                    _ => defaultIcon,
                },
                12 => today.Day switch
                {
                    >= 23 => PackIconKind.Snowman,
                    _ => defaultIcon,
                },
                _ => defaultIcon,
            };

            this.WhenAnyValue(x => x.SharedSettings.Backend)
                .Subscribe(_ =>
                {
                    Presets.Clear();
                    Presets.AddRange(SharedSettings.CustomPresets.AsEnumerable().Reverse().Where(x => (x.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend));
                    Presets.AddRange(Preset.PredefinedPresets.Where(x => (x.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend));
                });

            OutputTemplateHistory = [.. SharedSettings.OutputTemplateHistory.Select(x => new HistoryItemViewModel(x, DeleteOutputTemplateItem)).Reverse()];
            DownloadPathHistory = [.. SharedSettings.DownloadPathHistory.Select(x => new HistoryItemViewModel(x, DeleteDownloadPathItem)).Reverse()];
            DownloadArguments =
            [
                .. SharedSettings.BackendDownloadArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)),
                new AddArgumentViewModel(AddArgument),
            ];

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                _link = args[1];
            }

            var gdaA = this.WhenAnyValue(
                x => x.SharedSettings.Backend,
                x => x.SharedSettings.FfmpegPath,
                x => x.SharedSettings.Proxy,
                x => x.SharedSettings.SelectedPreset,
                (_, _, _, _) => Unit.Default);

            var gdaB = this.WhenAnyValue(
                x => x.SharedSettings.DownloadSubtitles,
                x => x.SharedSettings.DownloadSubtitlesAllLanguages,
                x => x.SharedSettings.DownloadAutoGeneratedSubtitles,
                x => x.SharedSettings.AddMetadata,
                x => x.SharedSettings.DownloadThumbnail,
                x => x.SharedSettings.DownloadPlaylist,
                x => x.PlaylistItems,
                (_, _, _, _, _, _, _) => Unit.Default);

            var gdaC = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomOutputTemplate,
                x => x.SharedSettings.CustomOutputTemplate,
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.DownloadPath,
                (_, _, _, _) => Unit.Default);

            Observable.Merge(gdaA, gdaB, gdaC)
                      .Throttle(TimeSpan.FromSeconds(0.25))
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => GenerateDownloadArguments());

            SharedSettings.BackendGlobalArguments.ToObservableChangeSet()
                                           .ObserveOn(RxApp.MainThreadScheduler)
                                           .Subscribe(_ => GenerateGlobalArguments());

            _canResetCustomOutputTemplate = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomOutputTemplate,
                x => x.SharedSettings.CustomOutputTemplate,
                (useTemplate, template) => useTemplate && template != Settings.DefaultCustomOutputTemplate);

            _canBrowseDownloadFolder = this.WhenAnyValue(x => x.SharedSettings.UseCustomPath);

            _canOpenDownloadFolder = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.DownloadPath,
                (useCustomPath, path) => useCustomPath && Directory.Exists(path));

            _canRun = this.WhenAnyValue(
                x => x.Link,
                x => x.BackendInstance.IsRunning,
                x => x.SharedSettings.UseCustomOutputTemplate,
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.CustomOutputTemplate,
                x => x.SharedSettings.DownloadPath,
                x => x.SharedSettings.IsDlBinaryValid,
                x => x.SharedSettings.IsFfmpegBinaryValid,
                (link, isRunning, useCustomOutputTemplate, useCustomPath, outputTemplate, downloadPath, isDlBinaryValid, isFfmpegBinaryValid) =>
                    !string.IsNullOrEmpty(link) &&
                    !isRunning &&
                    (!useCustomOutputTemplate || !string.IsNullOrEmpty(outputTemplate)) &&
                    (!useCustomPath || Directory.Exists(downloadPath)) &&
                    isDlBinaryValid &&
                    isFfmpegBinaryValid);

            var canAbort = this.WhenAnyValue(x => x.BackendInstance.IsRunning);

            _canEditOrDeletePreset = this.WhenAnyValue(
                x => x.SharedSettings.SelectedPreset,
                selectedPreset => selectedPreset is not null && !selectedPreset.IsPredefined);

            _canDuplicatePreset = this.WhenAnyValue(
                x => x.SharedSettings.SelectedPreset,
                (Preset? selectedPreset) => selectedPreset is not null);

            if (SharedSettings.IsDlBinaryValid)
            {
                if (SharedSettings.BackendAutoUpdate)
                {
                    _ = BackendInstance.UpdateAsync();
                }
            }
            else
            {
                openDialog(new GetStartedDialogViewModel(SharedSettings, closeDialog));
            }
        }

        private void AddCustomPreset(Preset preset)
        {
            SharedSettings.CustomPresets.Add(preset);

            if ((preset.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend)
            {
                Presets.Insert(0, preset);
                SharedSettings.SelectedPreset = Presets.First();
            }
        }

        private void EditCustomPreset(Preset preset)
        {
            DeleteCustomPreset();
            AddCustomPreset(preset);
        }

        [ReactiveCommand]
        private void OpenAddCustomPresetDialog() => _presetDialogVM.AddOrEditPreset(Preset.Empty, AddCustomPreset);

        [ReactiveCommand(CanExecute = nameof(_canEditOrDeletePreset))]
        private void OpenEditCustomPresetDialog() => _presetDialogVM.AddOrEditPreset(SharedSettings.SelectedPreset!, EditCustomPreset);

        [ReactiveCommand(CanExecute = nameof(_canDuplicatePreset))]
        private void DuplicatePreset()
        {
            var dupNum = 0;
            string dupName;
            var preset = SharedSettings.SelectedPreset!;

            do
            {
                dupNum++;
                dupName = $"{preset.Name} ({dupNum})";
            }
            while (Presets.Any(x => x.Name == dupName));

            AddCustomPreset(preset.Duplicate(dupName));
        }

        [ReactiveCommand(CanExecute = nameof(_canEditOrDeletePreset))]
        private void DeleteCustomPreset()
        {
            var preset = SharedSettings.SelectedPreset!;
            SharedSettings.CustomPresets.Remove(preset);
            Presets.Remove(preset);
            SharedSettings.SelectedPreset = Presets.First();
        }

        private void DeleteOutputTemplateItem(HistoryItemViewModel item)
        {
            SharedSettings.OutputTemplateHistory.Remove(item.Text);
            OutputTemplateHistory.Remove(item);
        }

        private void DeleteDownloadPathItem(HistoryItemViewModel item)
        {
            SharedSettings.DownloadPathHistory.Remove(item.Text);
            DownloadPathHistory.Remove(item);
        }

        private void UpdateOutputTemplateHistory()
        {
            if (!SharedSettings.OutputTemplateHistory.Contains(SharedSettings.CustomOutputTemplate))
            {
                SharedSettings.OutputTemplateHistory.Add(SharedSettings.CustomOutputTemplate);
                OutputTemplateHistory.Insert(0, new(SharedSettings.CustomOutputTemplate, DeleteOutputTemplateItem));
            }
        }

        private void UpdateDownloadPathHistory()
        {
            if (!SharedSettings.DownloadPathHistory.Contains(SharedSettings.DownloadPath))
            {
                SharedSettings.DownloadPathHistory.Add(SharedSettings.DownloadPath);
                DownloadPathHistory.Insert(0, new(SharedSettings.DownloadPath, DeleteDownloadPathItem));
            }
        }

        private void DeleteArgumentChip(ArgumentChipViewModel item)
        {
            if (item.IsRemovable)
            {
                SharedSettings.BackendDownloadArguments.Remove(item.Argument);
                DownloadArguments.Remove(item);
            }
        }

        private void AddArgument(string argument)
        {
            var backendArgument = new BackendArgument(argument);
            SharedSettings.BackendDownloadArguments.Add(backendArgument);

            // Insert right before AddArgumentViewModel.
            DownloadArguments.Insert(DownloadArguments.Count - 1, new ArgumentChipViewModel(backendArgument, true, DeleteArgumentChip));
        }

        [ReactiveCommand(CanExecute = nameof(_canResetCustomOutputTemplate))]
        private void ResetCustomOutputTemplate() => SharedSettings.CustomOutputTemplate = Settings.DefaultCustomOutputTemplate;

        [ReactiveCommand(CanExecute = nameof(_canBrowseDownloadFolder))]
        private void BrowseDownloadFolder()
        {
            Microsoft.Win32.OpenFolderDialog folderDialog = new()
            {
                ValidateNames = false,
                InitialDirectory = SharedSettings.DownloadPath,
            };

            var result = folderDialog.ShowDialog();
            if (result == true)
            {
                SharedSettings.DownloadPath = folderDialog.FolderName;
            }
        }

        [ReactiveCommand(CanExecute = nameof(_canOpenDownloadFolder))]
        private void OpenDownloadFolder()
        {
            try
            {
                WpfHelper.OpenUri(SharedSettings.DownloadPath);
            }
            catch (Exception ex)
            {
                _snackbarMessageQueue.Enqueue(ex.Message);
            }
        }

        private void GenerateGlobalArguments()
        {
            for (var i = 0; i < _globalArgCount; i++)
            {
                DownloadArguments.RemoveAt(0);
            }

            _globalArgCount = 0;

            foreach (var globalArg in SharedSettings.BackendGlobalArguments)
            {
                DownloadArguments.Insert(_globalArgCount, new ArgumentChipViewModel(globalArg, false, DeleteArgumentChip)); ;
                _globalArgCount++;
            }
        }

        /// <summary>
        /// Generates arguments for a download operation and
        /// updates the corresponding argument chips.
        /// </summary>
        private void GenerateDownloadArguments()
        {
            for (var i = 0; i < BackendInstance.GeneratedDownloadArguments.Count; i++)
            {
                DownloadArguments.RemoveAt(_globalArgCount);
            }

            BackendInstance.GenerateDownloadArguments(PlaylistItems);

            var pos = _globalArgCount;

            foreach (var arg in BackendInstance.GeneratedDownloadArguments)
            {
                DownloadArguments.Insert(pos, new ArgumentChipViewModel(new(arg), false, DeleteArgumentChip));
                pos++;
            }
        }

        [ReactiveCommand(CanExecute = nameof(_canRun))]
        private Task StartDownloadAsync(string link, CancellationToken cancellationToken = default)
        {
            if (SharedSettings.UseCustomOutputTemplate)
                UpdateOutputTemplateHistory();

            if (SharedSettings.UseCustomPath)
                UpdateDownloadPathHistory();

            return BackendInstance.StartDownloadAsync(link, cancellationToken);
        }

        [ReactiveCommand(CanExecute = nameof(_canRun))]
        private Task ListFormatsAsync(string link, CancellationToken cancellationToken = default) =>
            BackendInstance.ListFormatsAsync(link, cancellationToken);
    }
}
