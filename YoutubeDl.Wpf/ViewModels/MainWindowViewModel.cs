using MaterialDesignThemes.Wpf;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using YoutubeDl.Wpf.Views;

namespace YoutubeDl.Wpf.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _drawerItems = GenerateItems(snackbarMessageQueue);
            _selectedItem = _drawerItems.First();
            _openAboutDialog = new DelegateCommand(OnOpenAboutDialog, (object commandParameter) => true);
        }

        private ObservableCollection<DrawerItem> _drawerItems;
        private DrawerItem _selectedItem;

        private readonly DelegateCommand _openAboutDialog;

        public ICommand OpenAboutDialog => _openAboutDialog;

        private async void OnOpenAboutDialog(object commandParameter)
        {
            var aboutDialog = new AboutDialog
            {
                DataContext = this
            };
            await DialogHost.Show(aboutDialog, "RootDialog");
        }

        public ObservableCollection<DrawerItem> DrawerItems
        {
            get => _drawerItems;
            set => this.RaiseAndSetIfChanged(ref _drawerItems, value);
        }

        public DrawerItem SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        private ObservableCollection<DrawerItem> GenerateItems(ISnackbarMessageQueue snackbarMessageQueue)
        {
            if (snackbarMessageQueue == null)
                throw new ArgumentNullException(nameof(snackbarMessageQueue));

            return new ObservableCollection<DrawerItem>
            {
                new DrawerItem("Home", new HomeView(snackbarMessageQueue)),
                new DrawerItem("Settings", new SettingsView(snackbarMessageQueue))
            };
        }
    }
}
