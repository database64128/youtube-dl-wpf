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

        public Settings Settings { get; }

        public BackendService BackendService { get; }

        public BackendInstance BackendInstance { get; }

        public QueuedTextBoxSink QueuedTextBoxSink { get; }

        public PresetDialogViewModel PresetDialogVM { get; }

        public ObservableCollection<Preset> Presets { get; } = new();

        /// <summary>
        /// Gets the download path history.
        /// This collection was first constructed from <see cref="Settings.DownloadPathHistory"/> in reverse order.
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

        public HomeViewModel(Settings settings, BackendService backendService, QueuedTextBoxSink queuedTextBoxSink, PresetDialogViewModel presetDialogViewModel, ISnackbarMessageQueue snackbarMessageQueue)
        {
            Settings = settings;
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

            this.WhenAnyValue(x => x.Settings.Backend)
                .Subscribe(_ =>
                {
                    Presets.Clear();
                    Presets.AddRange(Settings.CustomPresets.Where(x => (x.SupportedBackends & Settings.Backend) == Settings.Backend));
                    Presets.AddRange(Preset.PredefinedPresets.Where(x => (x.SupportedBackends & Settings.Backend) == Settings.Backend));
                });

            DownloadPathHistory.AddRange(Settings.DownloadPathHistory.Select(x => new DownloadPathItemViewModel(x, DeleteDownloadPathItem)).Reverse());

            DownloadArguments.AddRange(Settings.BackendDownloadArguments.Select(x => new ArgumentChipViewModel(x, true, DeleteArgumentChip)));
            DownloadArguments.Add(new AddArgumentViewModel(AddArgument));

            var gdaA = this.WhenAnyValue(
                x => x.Settings.Backend,
                x => x.Settings.FfmpegPath,
                x => x.Settings.Proxy,
                x => x.Settings.SelectedPreset)
                .Select(_ => Unit.Default);

            var gdaB = this.WhenAnyValue(
                x => x.Settings.DownloadSubtitles,
                x => x.Settings.DownloadSubtitlesAllLanguages,
                x => x.Settings.DownloadAutoGeneratedSubtitles,
                x => x.Settings.AddMetadata,
                x => x.Settings.DownloadThumbnail,
                x => x.Settings.DownloadPlaylist)
                .Select(_ => Unit.Default);

            var gdaC = this.WhenAnyValue(
                x => x.Settings.UseCustomOutputTemplate,
                x => x.Settings.CustomOutputTemplate,
                x => x.Settings.UseCustomPath,
                x => x.Settings.DownloadPath)
                .Select(_ => Unit.Default);

            Observable.Merge(gdaA, gdaB, gdaC)
                      .Throttle(TimeSpan.FromSeconds(0.25))
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => GenerateDownloadArguments());

            Settings.BackendGlobalArguments.ToObservableChangeSet()
                                           .ObserveOn(RxApp.MainThreadScheduler)
                                           .Subscribe(_ => GenerateGlobalArguments());

            var canResetCustomFilenameTemplate = this.WhenAnyValue(
                x => x.Settings.UseCustomOutputTemplate,
                x => x.Settings.CustomOutputTemplate,
                (useTemplate, template) => useTemplate && template != Settings.DefaultCustomFilenameTemplate);

            var canBrowseDownloadFolder = this.WhenAnyValue(x => x.Settings.UseCustomPath);

            var canOpenDownloadFolder = this.WhenAnyValue(
                x => x.Settings.UseCustomPath,
                x => x.Settings.DownloadPath,
                (useCustomPath, path) => useCustomPath && Directory.Exists(path));

            var canStartDl = this.WhenAnyValue(
                x => x.Link,
                x => x.Settings.UseCustomPath,
                x => x.Settings.DownloadPath,
                x => x.Settings.BackendPath,
                x => x.BackendInstance.IsRunning,
                (link, useCustomPath, downloadPath, dlBinaryPath, isRunning) =>
                    !string.IsNullOrEmpty(link) &&
                    (!useCustomPath || Directory.Exists(downloadPath)) &&
                    !string.IsNullOrEmpty(dlBinaryPath) &&
                    !isRunning);

            var canAbortDl = this.WhenAnyValue(x => x.BackendInstance.IsRunning);

            var canEditOrDeletePreset = this.WhenAnyValue(
                x => x.Settings.SelectedPreset,
                selectedPreset => selectedPreset is not null && !selectedPreset.IsPredefined);

            var canDuplicatePreset = this.WhenAnyValue(
                x => x.Settings.SelectedPreset,
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

            if (Settings.BackendAutoUpdate && !string.IsNullOrEmpty(Settings.BackendPath))
            {
                BackendInstance.UpdateDl();
            }
        }

        private void AddCustomPreset(Preset preset)
        {
            Settings.CustomPresets.Add(preset);

            if ((preset.SupportedBackends & Settings.Backend) == Settings.Backend)
            {
                Presets.Insert(0, preset);
                Settings.SelectedPreset = Presets.First();
            }
        }

        private void EditCustomPreset(Preset preset)
        {
            DeleteCustomPreset();
            AddCustomPreset(preset);
        }

        private void OpenAddCustomPresetDialog() => PresetDialogVM.AddOrEditPreset(Preset.Auto, AddCustomPreset);

        private void OpenEditCustomPresetDialog() => PresetDialogVM.AddOrEditPreset(Settings.SelectedPreset!, EditCustomPreset);

        private void DuplicatePreset()
        {
            var dup = Settings.SelectedPreset! with { Name = $"{Settings.SelectedPreset.DisplayName} (1)", IsPredefined = false };
            AddCustomPreset(dup);
        }

        private void DeleteCustomPreset()
        {
            var preset = Settings.SelectedPreset!;
            Settings.CustomPresets.Remove(preset);
            Presets.Remove(preset);
            Settings.SelectedPreset = Presets.First();
        }

        private void DeleteDownloadPathItem(DownloadPathItemViewModel item)
        {
            Settings.DownloadPathHistory.Remove(item.Path);
            DownloadPathHistory.Remove(item);
        }

        private void UpdateDownloadPathHistory()
        {
            // No need to check if path is null or empty.
            // Because this code path can only be reached
            // when custom path is toggled and a valid path is supplied.
            if (!Settings.DownloadPathHistory.Contains(Settings.DownloadPath))
            {
                Settings.DownloadPathHistory.Add(Settings.DownloadPath);
                DownloadPathHistory.Insert(0, new(Settings.DownloadPath, DeleteDownloadPathItem));
            }
        }

        private void DeleteArgumentChip(ArgumentChipViewModel item)
        {
            if (item.IsRemovable)
            {
                Settings.BackendDownloadArguments.Remove(item.Argument);
                DownloadArguments.Remove(item);
            }
        }

        private void AddArgument(string argument)
        {
            var backendArgument = new BackendArgument(argument);
            Settings.BackendDownloadArguments.Add(backendArgument);

            // Insert right before AddArgumentViewModel.
            DownloadArguments.Insert(DownloadArguments.Count - 1, new ArgumentChipViewModel(backendArgument, true, DeleteArgumentChip));
        }

        private void ResetCustomFilenameTemplate()
        {
            Settings.CustomOutputTemplate = Settings.DefaultCustomFilenameTemplate;
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
                InitialDirectory = Settings.DownloadPath,
            };

            var result = folderDialog.ShowDialog();
            if (result == true)
            {
                Settings.DownloadPath = Path.GetDirectoryName(folderDialog.FileName) ?? "";
            }
        }

        private void OpenDownloadFolder()
        {
            try
            {
                WpfHelper.OpenUri(Settings.DownloadPath);
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

            foreach (var globalArg in Settings.BackendGlobalArguments)
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

            if (Settings.UseCustomPath)
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
