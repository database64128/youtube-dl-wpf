using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public class PresetDialogViewModel : ReactiveValidationObject
{
    private readonly List<BackendArgument> _backendArguments = new();
    private readonly AddArgumentViewModel _addArgumentViewModel;
    private readonly Action<bool> _controlDialogAction;
    private Action<Preset>? _saveAction;

    [Reactive]
    public string Name { get; set; } = "";

    [Reactive]
    public string FormatArg { get; set; } = "";

    [Reactive]
    public string ContainerArg { get; set; } = "";

    [Reactive]
    public bool IsYtdlSupported { get; set; } = true;

    [Reactive]
    public bool IsYtdlpSupported { get; set; } = true;

    public ObservableCollection<object> ArgumentChips { get; set; } = new();

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public ReactiveCommand<Unit, Unit> DiscardCommand { get; }

    public PresetDialogViewModel(Action<bool> controlDialogAction)
    {
        _addArgumentViewModel = new(AddArgument);
        _controlDialogAction = controlDialogAction;

        var canSave = this.WhenAnyValue(
            x => x.Name,
            x => x.IsYtdlSupported,
            x => x.IsYtdlpSupported,
            (name, ytdl, ytdlp) => !string.IsNullOrEmpty(name) && (ytdl || ytdlp));

        SaveCommand = ReactiveCommand.Create(Save, canSave);
        DiscardCommand = ReactiveCommand.Create(CloseDialog);

        this.WhenAnyValue(
            x => x.FormatArg,
            x => x.ContainerArg,
            x => x.IsYtdlSupported,
            x => x.IsYtdlpSupported)
            .Throttle(TimeSpan.FromSeconds(0.1))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => GenerateArgumentChips());
    }

    public void AddOrEditPreset(Preset preset, Action<Preset> saveAction)
    {
        _saveAction = saveAction;
        LoadPreset(preset);
        OpenDialog();
    }

    private void OpenDialog() => _controlDialogAction(true);

    private void CloseDialog() => _controlDialogAction(false);

    private void Save()
    {
        if (_saveAction is null)
        {
            throw new Exception("Property SaveAction is null.");
        }

        CloseDialog();
        _saveAction(ToPreset());
    }

    private void LoadPreset(Preset preset)
    {
        Name = preset.Name ?? "";
        FormatArg = preset.FormatArg ?? "";
        ContainerArg = preset.ContainerArg ?? "";
        IsYtdlSupported = (preset.SupportedBackends & BackendTypes.Ytdl) == BackendTypes.Ytdl;
        IsYtdlpSupported = (preset.SupportedBackends & BackendTypes.Ytdlp) == BackendTypes.Ytdlp;
        _backendArguments.Clear();
        _backendArguments.AddRange(preset.ExtraArgs.Select(x => new BackendArgument(x)));
    }

    private Preset ToPreset()
    {
        var supportedBackends = BackendTypes.None;
        if (IsYtdlSupported)
        {
            supportedBackends |= BackendTypes.Ytdl;
        }
        if (IsYtdlpSupported)
        {
            supportedBackends |= BackendTypes.Ytdlp;
        }

        return new(
            string.IsNullOrEmpty(Name) ? null : Name,
            string.IsNullOrEmpty(FormatArg) ? null : FormatArg,
            string.IsNullOrEmpty(ContainerArg) ? null : ContainerArg,
            supportedBackends,
            false,
            _backendArguments.Select(x => x.Argument).ToArray());
    }

    private void DeleteArgumentChip(ArgumentChipViewModel item)
    {
        if (item.IsRemovable)
        {
            _backendArguments.Remove(item.Argument);
            ArgumentChips.Remove(item);
        }
    }

    private void AddArgument(string argument)
    {
        var backendArgument = new BackendArgument(argument);
        _backendArguments.Add(backendArgument);

        // Insert right before AddArgumentViewModel.
        ArgumentChips.Insert(ArgumentChips.Count - 1, new ArgumentChipViewModel(backendArgument, true, DeleteArgumentChip));
    }

    /// <summary>
    /// Clears and regenerates all argument chips from scratch.
    /// </summary>
    private void GenerateArgumentChips()
    {
        ArgumentChips.Clear();
        ArgumentChips.AddRange(ToPreset().ToArgs().Select(x => new ArgumentChipViewModel(new(x), false, DeleteArgumentChip)));
        ArgumentChips.Add(_addArgumentViewModel);
    }
}
