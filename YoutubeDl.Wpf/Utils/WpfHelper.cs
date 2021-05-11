using System.Diagnostics;
using System.Windows.Controls;

namespace YoutubeDl.Wpf.Utils
{
    public static class WpfHelper
    {
        /// <summary>
        /// Opens a URI.
        /// For more information see https://github.com/dotnet/corefx/issues/10361.
        /// </summary>
        /// <param name="uri">The URI to open.</param>
        public static void OpenUri(string uri) => _ = Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

        /// <summary>
        /// Determines whether the TextBox is scrolled to the end.
        /// </summary>
        /// <param name="textBox">The TextBox.</param>
        /// <returns>
        /// True if the TextBox is scrolled to the end.
        /// Otherwise false.
        /// </returns>
        public static bool IsScrolledToEnd(TextBox textBox) => textBox.VerticalOffset > textBox.ExtentHeight - textBox.ViewportHeight - textBox.FontSize * textBox.FontFamily.LineSpacing;
    }
}
