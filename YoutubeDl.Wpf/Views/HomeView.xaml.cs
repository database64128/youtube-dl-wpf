using MaterialDesignThemes.Wpf;
using System.Windows.Controls;
using YoutubeDl.Wpf.ViewModels;

namespace YoutubeDl.Wpf.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView(ISnackbarMessageQueue snackbarMessageQueue)
        {
            InitializeComponent();
            _snackbarMessageQueue = snackbarMessageQueue;
            DataContext = new HomeViewModel(_snackbarMessageQueue);
        }

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;

        private static bool IsScrolledToEnd(TextBox textBox) => textBox.VerticalOffset > textBox.ExtentHeight - textBox.ViewportHeight - textBox.FontSize * textBox.FontFamily.LineSpacing;

        private void ResultTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsScrolledToEnd(resultTextBox))
                resultTextBox.ScrollToEnd();
        }
    }
}
