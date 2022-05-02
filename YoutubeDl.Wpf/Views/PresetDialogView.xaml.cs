using ReactiveUI;
using System.Reactive.Disposables;

namespace YoutubeDl.Wpf.Views;

/// <summary>
/// Interaction logic for PresetDialogView.xaml
/// </summary>
public partial class PresetDialogView
{
    public PresetDialogView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel,
                viewModel => viewModel.Name,
                view => view.nameTextBox.Text)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.FormatArg,
                view => view.formatTextBox.Text)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.ContainerArg,
                view => view.containerTextBox.Text)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.IsYtdlSupported,
                view => view.backendYoutubeDlCheckBox.IsChecked)
                .DisposeWith(disposables);

            this.Bind(ViewModel,
                viewModel => viewModel.IsYtdlpSupported,
                view => view.backendYtDlpCheckBox.IsChecked)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                viewModel => viewModel.ArgumentChips,
                view => view.argumentsItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.SaveCommand,
                view => view.saveButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.DiscardCommand,
                view => view.discardButton)
                .DisposeWith(disposables);
        });
    }
}
