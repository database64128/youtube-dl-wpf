using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public partial class PresetDialogViewModel : ReactiveObject
{
    private readonly List<BackendArgument> _backendArguments = [];
    private readonly Action<object?> _openDialog;
    private readonly Action _closeDialog;
    private readonly IObservable<bool> _canSave;
    private Preset? _preset;
    private Action<Preset>? _saveAction;

    [Reactive]
    private string _name = "";

    [Reactive]
    private string _formatArg = "";

    [Reactive]
    private string _containerArg = "";

    [Reactive]
    private bool _isYtdlSupported = true;

    [Reactive]
    private bool _isYtdlpSupported = true;

    public ObservableCollection<object> ArgumentChips { get; }

    public PresetDialogViewModel(Action<object?> openDialog, Action closeDialog, ReactiveCommand<Unit, Unit> closeDialogCommand)
    {
        _openDialog = openDialog;
        _closeDialog = closeDialog;
        CloseDialogCommand = closeDialogCommand;

        ArgumentChips =
        [
            new AddArgumentViewModel(AddArgument),
        ];

        _canSave = this.WhenAnyValue(
            x => x.Name,
            x => x.IsYtdlSupported,
            x => x.IsYtdlpSupported,
            (name, ytdl, ytdlp) => !string.IsNullOrEmpty(name) && (ytdl || ytdlp));

        this.WhenAnyValue(
            x => x.FormatArg,
            x => x.ContainerArg,
            x => x.IsYtdlSupported,
            x => x.IsYtdlpSupported)
            .Throttle(TimeSpan.FromSeconds(0.25))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(((string formatArg, string containerArg, bool isYtdlSupported, bool isYtdlpSupported) x) => UpdatePreset(x.formatArg, x.containerArg, x.isYtdlSupported, x.isYtdlpSupported));
    }

    public void AddOrEditPreset(Preset preset, Action<Preset> saveAction)
    {
        _preset = preset;
        _saveAction = saveAction;
        LoadPreset(preset);
        _openDialog(this);
    }

    public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; }

    [ReactiveCommand(CanExecute = nameof(_canSave))]
    private void Save()
    {
        if (_saveAction is null)
        {
            throw new InvalidOperationException("Missing save action for preset.");
        }

        _closeDialog();
        _saveAction(ToPreset());
    }

    private void LoadPresetExtraArgs(Preset preset)
    {
        // Calculate index of first extra argument chip.
        var index = ArgumentChips.Count - _backendArguments.Count - 1;

        // Remove extra argument chips.
        for (var i = 0; i < _backendArguments.Count; i++)
        {
            ArgumentChips.RemoveAt(index);
        }

        // Clear extra arguments.
        _backendArguments.Clear();

        // Add new extra arguments.
        foreach (var extraArg in preset.ExtraArgs)
        {
            AddArgument(extraArg);
        }
    }

    private void LoadPreset(Preset preset)
    {
        LoadPresetExtraArgs(preset);
        Name = preset.Name ?? "";
        FormatArg = preset.FormatArg ?? "";
        ContainerArg = preset.ContainerArg ?? "";
        IsYtdlSupported = (preset.SupportedBackends & BackendTypes.Ytdl) == BackendTypes.Ytdl;
        IsYtdlpSupported = (preset.SupportedBackends & BackendTypes.Ytdlp) == BackendTypes.Ytdlp;
    }

    private Preset ToPreset()
    {
        if (_preset is null)
        {
            throw new InvalidOperationException("Preset is not loaded.");
        }

        var supportedBackends = BackendTypes.None;
        if (IsYtdlSupported)
        {
            supportedBackends |= BackendTypes.Ytdl;
        }
        if (IsYtdlpSupported)
        {
            supportedBackends |= BackendTypes.Ytdlp;
        }

        return _preset with
        {
            Name = Name,
            FormatArg = string.IsNullOrEmpty(FormatArg) ? null : FormatArg,
            ContainerArg = string.IsNullOrEmpty(ContainerArg) ? null : ContainerArg,
            SupportedBackends = supportedBackends,
            ExtraArgs = [.. _backendArguments.Select(x => x.Argument)],
        };
    }

    private void UpdatePreset(string formatArg, string containerArg, bool isYtdlSupported, bool isYtdlpSupported)
    {
        if (_preset is null)
        {
            return;
        }

        var supportedBackends = BackendTypes.None;
        if (isYtdlSupported)
        {
            supportedBackends |= BackendTypes.Ytdl;
        }
        if (isYtdlpSupported)
        {
            supportedBackends |= BackendTypes.Ytdlp;
        }

        _preset = _preset with
        {
            FormatArg = string.IsNullOrEmpty(formatArg) ? null : formatArg,
            ContainerArg = string.IsNullOrEmpty(containerArg) ? null : containerArg,
            SupportedBackends = supportedBackends,
        };

        GenerateArgumentChips(_preset);
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
    /// Generates and updates non-extra argument chips.
    /// </summary>
    private void GenerateArgumentChips(Preset preset)
    {
        while (ArgumentChips.Count - _backendArguments.Count - 1 > 0)
        {
            ArgumentChips.RemoveAt(0);
        }

        var index = 0;

        foreach (var arg in preset.GetNonExtraArgs())
        {
            ArgumentChips.Insert(index, new ArgumentChipViewModel(new(arg), false, DeleteArgumentChip));
            index++;
        }
    }
}
