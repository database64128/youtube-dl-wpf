using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace YoutubeDl.Wpf.Views;

/// <summary>
/// Interaction logic for ArgumentChipView.xaml
/// </summary>
public partial class ArgumentChipView
{
    public ArgumentChipView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Chip text
            this.OneWayBind(ViewModel,
                viewModel => viewModel.Argument.Argument,
                view => view.argumentChip.Content)
                .DisposeWith(disposables);

            // Chip remove button
            this.OneWayBind(ViewModel,
                viewModel => viewModel.IsRemovable,
                view => view.argumentChip.IsDeletable)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.RemoveArgumentCommand,
                view => view.argumentChip,
                Observable.Return(ViewModel),
                nameof(argumentChip.DeleteClick))
                .DisposeWith(disposables);
        });
    }
}
