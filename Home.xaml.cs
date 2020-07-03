using MaterialDesignThemes.Wpf;
using System.Windows.Controls;

namespace youtube_dl_wpf
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        public Home(ISnackbarMessageQueue snackbarMessageQueue)
        {
            InitializeComponent();
            _snackbarMessageQueue = snackbarMessageQueue;
            DataContext = new HomeViewModel(_snackbarMessageQueue);
        }

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;

        private bool IsScrolledToEnd(TextBox textBox) => textBox.VerticalOffset > textBox.ExtentHeight - textBox.ViewportHeight - textBox.FontSize * textBox.FontFamily.LineSpacing;

        private void resultTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsScrolledToEnd(resultTextBox))
                resultTextBox.ScrollToEnd();
        }
    }
}
