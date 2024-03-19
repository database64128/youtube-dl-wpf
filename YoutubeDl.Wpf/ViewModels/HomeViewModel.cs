using DynamicData;
using DynamicData.Binding;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
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
    public class HomeViewModel : ReactiveValidationObject
    {
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private int _globalArgCount;

        public PackIconKind TabItemHeaderIconKind { get; }

        public ObservableSettings SharedSettings { get; }

        public BackendService BackendService { get; }

        public BackendInstance BackendInstance { get; }

        public QueuedTextBoxSink QueuedTextBoxSink { get; }

        public PresetDialogViewModel PresetDialogVM { get; }

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
        public string Link { get; set; } = "";

        [Reactive]
        public string PlaylistItems { get; set; } = "";

        public ReactiveCommand<Unit, Unit> ResetCustomOutputTemplateCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDownloadFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenDownloadFolderCommand { get; }
        public ReactiveCommand<string, Unit> StartDownloadCommand { get; }
        public ReactiveCommand<string, Unit> ListFormatsCommand { get; }
        public ReactiveCommand<Unit, Unit> AbortCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenAddCustomPresetDialogCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenEditCustomPresetDialogCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicatePresetCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteCustomPresetCommand { get; }

        public HomeViewModel(ObservableSettings settings, BackendService backendService, QueuedTextBoxSink queuedTextBoxSink, PresetDialogViewModel presetDialogViewModel, ISnackbarMessageQueue snackbarMessageQueue)
        {
            SharedSettings = settings;
            BackendService = backendService;
            BackendInstance = backendService.CreateInstance();
            QueuedTextBoxSink = queuedTextBoxSink;
            PresetDialogVM = presetDialogViewModel;
            _snackbarMessageQueue = snackbarMessageQueue;

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

            OutputTemplateHistory = new(SharedSettings.OutputTemplateHistory.Select(x => new HistoryItemViewModel(x, DeleteOutputTemplateItem)).Reverse());
            DownloadPathHistory = new(SharedSettings.DownloadPathHistory.Select(x => new HistoryItemViewModel(x, DeleteDownloadPathItem)).Reverse());
            DownloadArguments = new(SharedSettings.BackendDownloadArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)))
            {
                new AddArgumentViewModel(AddArgument),
            };

            var gdaA = this.WhenAnyValue(
                x => x.SharedSettings.Backend,
                x => x.SharedSettings.FfmpegPath,
                x => x.SharedSettings.Proxy,
                x => x.SharedSettings.SelectedPreset)
                .Select(_ => Unit.Default);

            var gdaB = this.WhenAnyValue(
                x => x.SharedSettings.DownloadSubtitles,
                x => x.SharedSettings.DownloadSubtitlesAllLanguages,
                x => x.SharedSettings.DownloadAutoGeneratedSubtitles,
                x => x.SharedSettings.AddMetadata,
                x => x.SharedSettings.DownloadThumbnail,
                x => x.SharedSettings.DownloadPlaylist,
                x => x.PlaylistItems)
                .Select(_ => Unit.Default);

            var gdaC = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomOutputTemplate,
                x => x.SharedSettings.CustomOutputTemplate,
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.DownloadPath)
                .Select(_ => Unit.Default);

            Observable.Merge(gdaA, gdaB, gdaC)
                      .Throttle(TimeSpan.FromSeconds(0.25))
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => GenerateDownloadArguments());

            SharedSettings.BackendGlobalArguments.ToObservableChangeSet()
                                           .ObserveOn(RxApp.MainThreadScheduler)
                                           .Subscribe(_ => GenerateGlobalArguments());

            var canResetCustomOutputTemplate = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomOutputTemplate,
                x => x.SharedSettings.CustomOutputTemplate,
                (useTemplate, template) => useTemplate && template != Settings.DefaultCustomOutputTemplate);

            var canBrowseDownloadFolder = this.WhenAnyValue(x => x.SharedSettings.UseCustomPath);

            var canOpenDownloadFolder = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.DownloadPath,
                (useCustomPath, path) => useCustomPath && Directory.Exists(path));

            var canRun = this.WhenAnyValue(
                x => x.Link,
                x => x.BackendInstance.IsRunning,
                x => x.SharedSettings.UseCustomOutputTemplate,
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.CustomOutputTemplate,
                x => x.SharedSettings.DownloadPath,
                x => x.SharedSettings.BackendPath,
                (link, isRunning, useCustomOutputTemplate, useCustomPath, outputTemplate, downloadPath, backendPath) =>
                    !string.IsNullOrEmpty(link) &&
                    !isRunning &&
                    (!useCustomOutputTemplate || !string.IsNullOrEmpty(outputTemplate)) &&
                    (!useCustomPath || Directory.Exists(downloadPath)) &&
                    !string.IsNullOrEmpty(backendPath));

            var canAbort = this.WhenAnyValue(x => x.BackendInstance.IsRunning);

            var canEditOrDeletePreset = this.WhenAnyValue(
                x => x.SharedSettings.SelectedPreset,
                selectedPreset => selectedPreset is not null && !selectedPreset.IsPredefined);

            var canDuplicatePreset = this.WhenAnyValue(
                x => x.SharedSettings.SelectedPreset,
                (Preset? selectedPreset) => selectedPreset is not null);

            ResetCustomOutputTemplateCommand = ReactiveCommand.Create(ResetCustomOutputTemplate, canResetCustomOutputTemplate);
            BrowseDownloadFolderCommand = ReactiveCommand.Create(BrowseDownloadFolder, canBrowseDownloadFolder);
            OpenDownloadFolderCommand = ReactiveCommand.Create(OpenDownloadFolder, canOpenDownloadFolder);
            StartDownloadCommand = ReactiveCommand.CreateFromTask<string>(StartDownloadAsync, canRun);
            ListFormatsCommand = ReactiveCommand.CreateFromTask<string>(BackendInstance.ListFormatsAsync, canRun);
            AbortCommand = ReactiveCommand.CreateFromTask(BackendInstance.AbortAsync, canAbort);

            OpenAddCustomPresetDialogCommand = ReactiveCommand.Create(OpenAddCustomPresetDialog);
            OpenEditCustomPresetDialogCommand = ReactiveCommand.Create(OpenEditCustomPresetDialog, canEditOrDeletePreset);
            DuplicatePresetCommand = ReactiveCommand.Create(DuplicatePreset, canDuplicatePreset);
            DeleteCustomPresetCommand = ReactiveCommand.Create(DeleteCustomPreset, canEditOrDeletePreset);

            if (SharedSettings.BackendAutoUpdate && !string.IsNullOrEmpty(SharedSettings.BackendPath))
            {
                _ = BackendInstance.UpdateAsync();
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

        private void OpenAddCustomPresetDialog() => PresetDialogVM.AddOrEditPreset(Preset.Empty, AddCustomPreset);

        private void OpenEditCustomPresetDialog() => PresetDialogVM.AddOrEditPreset(SharedSettings.SelectedPreset!, EditCustomPreset);

        private void DuplicatePreset()
        {
            var dupNum = 0;
            string dupName;
            var preset = SharedSettings.SelectedPreset!;

            do
            {
                dupNum++;
                dupName = $"{preset.DisplayName} ({dupNum})";
            }
            while (Presets.Any(x => x.DisplayName == dupName));

            AddCustomPreset(preset with { Name = dupName, IsPredefined = false });
        }

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

        private void ResetCustomOutputTemplate() => SharedSettings.CustomOutputTemplate = Settings.DefaultCustomOutputTemplate;

        private void BrowseDownloadFolder()
        {
            Microsoft.Win32.OpenFileDialog folderDialog = new()
            {
                FileName = "Folder Selection.",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                InitialDirectory = SharedSettings.DownloadPath,
            };

            var result = folderDialog.ShowDialog();
            if (result == true)
            {
                SharedSettings.DownloadPath = Path.GetDirectoryName(folderDialog.FileName) ?? "";
            }
        }

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

        private Task StartDownloadAsync(string link, CancellationToken cancellationToken = default)
        {
            if (SharedSettings.UseCustomOutputTemplate)
                UpdateOutputTemplateHistory();

            if (SharedSettings.UseCustomPath)
                UpdateDownloadPathHistory();

            return BackendInstance.StartDownloadAsync(link, cancellationToken);
        }
    }
}
