using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace YoutubeDl.Wpf.Views;

/// <summary>
/// Interaction logic for DownloadPathItemView.xaml
/// </summary>
public partial class DownloadPathItemView
{
    public DownloadPathItemView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel,
                viewModel => viewModel.Path,
                view => view.pathTextBlock.Text)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.DeleteItemCommand,
                view => view.deleteButton,
                Observable.Return(ViewModel))
                .DisposeWith(disposables);
        });
    }
}
