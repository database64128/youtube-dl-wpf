using ReactiveUI.Primitives.ObservableEvents;
using ReactiveUI;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives;
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
                           .Keep(x => x.Key == Key.Enter)
                           .Map(_ => ViewModel!.Argument)
                           .InvokeCommand(ViewModel!.AddArgumentCommand) // Null forgiving reason: upstream limitation.
                           .DisposeWith(disposables);
        });
    }
}
