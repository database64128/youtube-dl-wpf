using MaterialDesignThemes.Wpf;
using System.Windows.Controls;

namespace youtube_dl_wpf
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings(ISnackbarMessageQueue snackbarMessageQueue)
        {
            InitializeComponent();
            _snackbarMessageQueue = snackbarMessageQueue;
            DataContext = new SettingsViewModel(_snackbarMessageQueue);
        }

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
    }
}
