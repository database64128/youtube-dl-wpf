using ReactiveUI;
using System.Reactive.Disposables;
using YoutubeDl.Wpf.Models;

namespace YoutubeDl.Wpf.Views;

/// <summary>
/// Interaction logic for GetStartedDialogView.xaml
/// </summary>
public partial class GetStartedDialogView
{
    public GetStartedDialogView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel,
                viewModel => viewModel.SharedSettings.Backend,
                view => view.ytdlBackendTypeRadioButton.IsChecked,
                backend => backend == BackendTypes.Ytdl,
                isChecked => isChecked == true ? BackendTypes.Ytdl : BackendTypes.Ytdlp)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                viewModel => viewModel.SharedSettings.Backend,
                view => view.ytdlpBackendTypeRadioButton.IsChecked,
                backend => backend == BackendTypes.Ytdlp)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.SharedSettings.BackendPath,
                view => view.dlPathTextBox.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                viewModel => viewModel.SharedSettings.IsDlBinaryHintVisible,
                view => view.dlPathHintTextBlock.Visibility)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.SharedSettings.BackendAutoUpdate,
                view => view.autoUpdateDlToggle.IsChecked)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.SharedSettings.FfmpegPath,
                view => view.ffmpegPathTextBox.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                viewModel => viewModel.SharedSettings.IsFfmpegBinaryValid,
                view => view.ffmpegPathHintTextBlock.Visibility,
                conversionHint: BooleanToVisibilityHint.Inverse)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.SharedSettings.BrowseDlBinaryCommand,
                view => view.dlPathBrowseButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.SharedSettings.BrowseFfmpegBinaryCommand,
                view => view.ffmpegPathBrowseButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.CopyWingetInstallCommand,
                view => view.copyWingetInstallButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.CloseDialogCommand,
                view => view.goButton)
                .DisposeWith(disposables);
        });
    }
}
