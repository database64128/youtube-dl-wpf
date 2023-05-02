using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace YoutubeDl.Wpf.Views;

/// <summary>
/// Interaction logic for HistoryItemView.xaml
/// </summary>
public partial class HistoryItemView
{
    public HistoryItemView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel,
                viewModel => viewModel.Text,
                view => view.textBlock.Text)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.DeleteItemCommand,
                view => view.deleteButton,
                Observable.Return(ViewModel))
                .DisposeWith(disposables);
        });
    }
}
