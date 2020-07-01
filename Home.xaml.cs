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

        private bool IsScrolledToEnd(TextBox textBox) => textBox.VerticalOffset > textBox.ExtentHeight - textBox.ViewportHeight - textBox.FontSize * textBox.FontFamily.LineSpacing;

        private void resultTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsScrolledToEnd(resultTextBox))
                resultTextBox.ScrollToEnd();
        }
    }
}
