using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;

namespace YoutubeDl.Wpf.Views;

/// <summary>
/// Interaction logic for AddArgumentView.xaml
/// </summary>
public partial class AddArgumentView
{
    public AddArgumentView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel,
                viewModel => viewModel.Argument,
                view => view.argumentTextBox.Text)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                viewModel => viewModel.AddArgumentCommand,
                view => view.addButton,
                viewModel => viewModel.Argument)
                .DisposeWith(disposables);

            argumentTextBox.Events().KeyDown
                           .Where(x => x.Key == Key.Enter)
                           .Select(_ => ViewModel!.Argument)
                           .InvokeCommand(ViewModel!.AddArgumentCommand) // Null forgiving reason: upstream limitation.
                           .DisposeWith(disposables);
        });
    }
}
