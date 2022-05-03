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

        public ObservableCollection<Preset> Presets { get; } = new();

        /// <summary>
        /// Gets the download path history.
        /// This collection was first constructed from <see cref="ObservableSettings.DownloadPathHistory"/> in reverse order.
        /// So the newest path is always the first element.
        /// </summary>
        public ObservableCollection<DownloadPathItemViewModel> DownloadPathHistory { get; } = new();

        /// <summary>
        /// Gets the collection of view models of the arguments area.
        /// A view model in this collection must be of either
        /// <see cref="ArgumentChipViewModel"/> or <see cref="AddArgumentViewModel"/> type.
        /// </summary>
        public ObservableCollection<object> DownloadArguments { get; } = new();

        [Reactive]
        public string Link { get; set; } = "";

        public ReactiveCommand<Unit, Unit> ResetCustomFilenameTemplateCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDownloadFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenDownloadFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> StartDownloadCommand { get; }
        public ReactiveCommand<Unit, Unit> ListFormatsCommand { get; }
        public ReactiveCommand<Unit, Unit> AbortDlCommand { get; }

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
                    Presets.AddRange(SharedSettings.CustomPresets.Where(x => (x.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend));
                    Presets.AddRange(Preset.PredefinedPresets.Where(x => (x.SupportedBackends & SharedSettings.Backend) == SharedSettings.Backend));
                });

            DownloadPathHistory.AddRange(SharedSettings.DownloadPathHistory.Select(x => new DownloadPathItemViewModel(x, DeleteDownloadPathItem)).Reverse());

            DownloadArguments.AddRange(SharedSettings.BackendDownloadArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)));
            DownloadArguments.Add(new AddArgumentViewModel(AddArgument));

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
                x => x.SharedSettings.DownloadPlaylist)
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

            var canResetCustomFilenameTemplate = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomOutputTemplate,
                x => x.SharedSettings.CustomOutputTemplate,
                (useTemplate, template) => useTemplate && template != Settings.DefaultCustomFilenameTemplate);

            var canBrowseDownloadFolder = this.WhenAnyValue(x => x.SharedSettings.UseCustomPath);

            var canOpenDownloadFolder = this.WhenAnyValue(
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.DownloadPath,
                (useCustomPath, path) => useCustomPath && Directory.Exists(path));

            var canStartDl = this.WhenAnyValue(
                x => x.Link,
                x => x.SharedSettings.UseCustomPath,
                x => x.SharedSettings.DownloadPath,
                x => x.SharedSettings.BackendPath,
                x => x.BackendInstance.IsRunning,
                (link, useCustomPath, downloadPath, dlBinaryPath, isRunning) =>
                    !string.IsNullOrEmpty(link) &&
                    (!useCustomPath || Directory.Exists(downloadPath)) &&
                    !string.IsNullOrEmpty(dlBinaryPath) &&
                    !isRunning);

            var canAbortDl = this.WhenAnyValue(x => x.BackendInstance.IsRunning);

            var canEditOrDeletePreset = this.WhenAnyValue(
                x => x.SharedSettings.SelectedPreset,
                selectedPreset => selectedPreset is not null && !selectedPreset.IsPredefined);

            var canDuplicatePreset = this.WhenAnyValue(
                x => x.SharedSettings.SelectedPreset,
                selectedPreset => selectedPreset is not null && selectedPreset != Preset.Auto);

            ResetCustomFilenameTemplateCommand = ReactiveCommand.Create(ResetCustomFilenameTemplate, canResetCustomFilenameTemplate);
            BrowseDownloadFolderCommand = ReactiveCommand.Create(BrowseDownloadFolder, canBrowseDownloadFolder);
            OpenDownloadFolderCommand = ReactiveCommand.Create(OpenDownloadFolder, canOpenDownloadFolder);
            StartDownloadCommand = ReactiveCommand.Create(StartDownload, canStartDl);
            ListFormatsCommand = ReactiveCommand.Create(ListFormats, canStartDl);
            AbortDlCommand = ReactiveCommand.CreateFromTask(BackendInstance.AbortDl, canAbortDl);

            OpenAddCustomPresetDialogCommand = ReactiveCommand.Create(OpenAddCustomPresetDialog);
            OpenEditCustomPresetDialogCommand = ReactiveCommand.Create(OpenEditCustomPresetDialog, canEditOrDeletePreset);
            DuplicatePresetCommand = ReactiveCommand.Create(DuplicatePreset, canDuplicatePreset);
            DeleteCustomPresetCommand = ReactiveCommand.Create(DeleteCustomPreset, canEditOrDeletePreset);

            if (SharedSettings.BackendAutoUpdate && !string.IsNullOrEmpty(SharedSettings.BackendPath))
            {
                BackendInstance.UpdateDl();
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

        private void OpenAddCustomPresetDialog() => PresetDialogVM.AddOrEditPreset(Preset.Auto, AddCustomPreset);

        private void OpenEditCustomPresetDialog() => PresetDialogVM.AddOrEditPreset(SharedSettings.SelectedPreset!, EditCustomPreset);

        private void DuplicatePreset()
        {
            var dup = SharedSettings.SelectedPreset! with { Name = $"{SharedSettings.SelectedPreset.DisplayName} (1)", IsPredefined = false };
            AddCustomPreset(dup);
        }

        private void DeleteCustomPreset()
        {
            var preset = SharedSettings.SelectedPreset!;
            SharedSettings.CustomPresets.Remove(preset);
            Presets.Remove(preset);
            SharedSettings.SelectedPreset = Presets.First();
        }

        private void DeleteDownloadPathItem(DownloadPathItemViewModel item)
        {
            SharedSettings.DownloadPathHistory.Remove(item.Path);
            DownloadPathHistory.Remove(item);
        }

        private void UpdateDownloadPathHistory()
        {
            // No need to check if path is null or empty.
            // Because this code path can only be reached
            // when custom path is toggled and a valid path is supplied.
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

        private void ResetCustomFilenameTemplate()
        {
            SharedSettings.CustomOutputTemplate = Settings.DefaultCustomFilenameTemplate;
        }

        private void StartDownload()
        {
            BackendInstance.StartDownload(Link);
        }

        private void ListFormats()
        {
            BackendInstance.ListFormats(Link);
        }

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
            for (var i = _globalArgCount; i < _globalArgCount + BackendInstance.GeneratedDownloadArguments.Count; i++)
            {
                DownloadArguments.RemoveAt(_globalArgCount);
            }

            BackendInstance.GenerateDownloadArguments();

            if (SharedSettings.UseCustomPath)
            {
                UpdateDownloadPathHistory();
            }

            var pos = _globalArgCount;

            foreach (var arg in BackendInstance.GeneratedDownloadArguments)
            {
                DownloadArguments.Insert(pos, new ArgumentChipViewModel(new(arg), false, DeleteArgumentChip));
                pos++;
            }
        }
    }
}
