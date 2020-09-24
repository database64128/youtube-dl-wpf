using System.Windows.Controls;
using System.Windows.Navigation;
using YoutubeDl.Wpf.Behaviors;

namespace YoutubeDl.Wpf.Views
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : UserControl
    {
        public AboutDialog()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Utilities.OpenLink(e.Uri.AbsoluteUri);
        }
    }
}
