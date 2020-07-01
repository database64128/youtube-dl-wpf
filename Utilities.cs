using System.Diagnostics;
using System.Runtime.InteropServices;

namespace youtube_dl_wpf
{
    public static class Utilities
    {
        /// <summary>
        /// Open an URL.
        /// Workaround for https://github.com/dotnet/corefx/issues/10361.
        /// </summary>
        /// <param name="url">URL to open.</param>
        public static void OpenLink(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
