using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MaterialDesignThemes.Wpf;

namespace youtube_dl_wpf
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _drawerItems = GenerateItems(snackbarMessageQueue);
        }
        
        private ObservableCollection<DrawerItem> _drawerItems;
        private DrawerItem _selectedItem;

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
                new DrawerItem("Home", new Home()),
                new DrawerItem("Settings", new Settings())
            };
        }
    }
}
