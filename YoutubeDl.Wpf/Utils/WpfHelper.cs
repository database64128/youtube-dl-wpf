using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Windows.Win32;

namespace YoutubeDl.Wpf.Utils
{
    public static class WpfHelper
    {
        /// <summary>
        /// Opens a URI.
        /// For more information see https://github.com/dotnet/runtime/issues/17938.
        /// </summary>
        /// <param name="uri">The URI to open.</param>
        public static void OpenUri(string uri) => _ = Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

        /// <summary>
        /// Opens File Explorer with the specified file or directory selected.
        /// </summary>
        /// <remarks>This method uses Windows Shell APIs to locate and highlight the specified file or
        /// directory in its containing folder. If the specified path does not exist or is invalid, the method will have
        /// no effect.</remarks>
        /// <param name="path">The path to the file or directory to be shown in its containing folder.</param>
        public static unsafe void ShowInFolder(string path)
        {
            if (!Path.Exists(path))
            {
                return;
            }
            path = Path.GetFullPath(path);

            var pidl = PInvoke.ILCreateFromPath(path);
            if (pidl is null)
            {
                return;
            }

            try
            {
                PInvoke.SHOpenFolderAndSelectItems(pidl, 0u, null, 0u);
            }
            finally
            {
                PInvoke.ILFree(pidl);
            }
        }

        /// <summary>
        /// Determines whether the TextBox is scrolled to the end.
        /// This used to be based on https://stackoverflow.com/questions/14902394/get-the-scroll-position-of-a-wpf-textbox,
        /// but it stopped working sometime after v1.12.2, because the calculated value became a bit off.
        /// We now use a pre-calculated magic number. This needs to be adjusted if the font changes.
        /// </summary>
        /// <param name="textBox">The TextBox.</param>
        /// <returns>
        /// True if the TextBox is scrolled to the end.
        /// Otherwise false.
        /// </returns>
        public static bool IsScrolledToEnd(TextBox textBox) => textBox.VerticalOffset > textBox.ExtentHeight - textBox.ViewportHeight - 17.15;
    }
}
