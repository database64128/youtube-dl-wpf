using MaterialDesignThemes.Wpf;
using ReactiveUI;
using YoutubeDl.Wpf.Views;

namespace YoutubeDl.Wpf.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public HomeView GetHomeView { get; } = new();

        public SettingsView GetSettingsView { get; } = new();

        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            GetHomeView.ViewModel = new(snackbarMessageQueue);
            GetSettingsView.ViewModel = new(snackbarMessageQueue);
        }
    }
}
