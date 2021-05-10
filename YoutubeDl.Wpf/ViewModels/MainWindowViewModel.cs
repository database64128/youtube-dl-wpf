using MaterialDesignThemes.Wpf;
using ReactiveUI;
using YoutubeDl.Wpf.Views;

namespace YoutubeDl.Wpf.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public HomeView GetHomeView { get; }

        public SettingsView GetSettingsView { get; }

        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            GetHomeView = new(snackbarMessageQueue);
            GetSettingsView = new(snackbarMessageQueue);
        }
    }
}
