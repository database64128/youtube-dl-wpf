using MaterialDesignThemes.Wpf;
using System.Windows.Controls;
using YoutubeDl.Wpf.ViewModels;

namespace YoutubeDl.Wpf.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView(ISnackbarMessageQueue snackbarMessageQueue)
        {
            InitializeComponent();
            _snackbarMessageQueue = snackbarMessageQueue;
            DataContext = new SettingsViewModel(_snackbarMessageQueue);
        }

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
    }
}
