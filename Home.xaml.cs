using System.Windows.Controls;

namespace youtube_dl_wpf
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        public Home()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();
        }

        private void resultTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            resultTextBox.ScrollToEnd();
        }
    }
}
