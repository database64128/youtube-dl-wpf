using System.Windows.Controls;

namespace youtube_dl_wpf
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
