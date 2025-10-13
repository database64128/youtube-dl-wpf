using ReactiveUI;
using System.Reactive.Disposables.Fluent;
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

            this.OneWayBind(ViewModel,
                viewModel => viewModel.IsDeleteButtonVisible,
                view => view.deleteButton.Visibility)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.DeleteItemCommand,
                view => view.deleteButton,
                Observable.Return(ViewModel))
                .DisposeWith(disposables);
        });
    }
}
