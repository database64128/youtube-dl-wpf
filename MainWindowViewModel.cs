using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace youtube_dl_wpf
{
    public class MainWindowViewModel : ViewModelBase
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
            set => SetProperty(ref _drawerItems, value);
        }

        public DrawerItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private ObservableCollection<DrawerItem> GenerateItems(ISnackbarMessageQueue snackbarMessageQueue)
        {
            if (snackbarMessageQueue == null)
                throw new ArgumentNullException(nameof(snackbarMessageQueue));

            return new ObservableCollection<DrawerItem>
            {
                new DrawerItem("Home", new Home(snackbarMessageQueue)),
                new DrawerItem("Settings", new Settings(snackbarMessageQueue))
            };
        }
    }
}
