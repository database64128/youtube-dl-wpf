using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;
using System.Windows;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.ViewModels;

public partial class GetStartedDialogViewModel(ObservableSettings settings, Action closeDialog) : ReactiveObject
{
    public ObservableSettings SharedSettings { get; } = settings;

    public ReactiveCommand<RxVoid, RxVoid> CloseDialogCommand { get; } = ReactiveCommand.Create(closeDialog, settings.IsDlBinaryValidObservable);

    [ReactiveCommand]
    private static void CopyWingetInstall() => Clipboard.SetText(WingetInstallCommandText);

    public const string WingetInstallCommandText = "winget install yt-dlp 'FFmpeg (Shared)'";
}
