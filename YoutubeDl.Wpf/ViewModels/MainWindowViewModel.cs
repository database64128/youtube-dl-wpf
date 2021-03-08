using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using YoutubeDl.Wpf.Views;

namespace YoutubeDl.Wpf.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            DrawerItems = GenerateItems(snackbarMessageQueue);
            SelectedItem = DrawerItems.First();
            OpenAboutDialog = ReactiveCommand.CreateFromTask(OnOpenAboutDialog);
        }

        public ReactiveCommand<Unit, Unit> OpenAboutDialog { get; }

        private async Task OnOpenAboutDialog()
        {
            var aboutDialog = new AboutDialog
            {
                DataContext = this
            };
            await DialogHost.Show(aboutDialog, "RootDialog");
        }

        [Reactive]
        public ObservableCollection<DrawerItem> DrawerItems { get; set; }

        [Reactive]
        public DrawerItem SelectedItem { get; set; }

        private static ObservableCollection<DrawerItem> GenerateItems(ISnackbarMessageQueue snackbarMessageQueue) => new()
        {
            new DrawerItem("Home", new HomeView(snackbarMessageQueue)),
            new DrawerItem("Settings", new SettingsView(snackbarMessageQueue)),
        };
    }
}
