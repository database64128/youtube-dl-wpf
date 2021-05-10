using System.Diagnostics;

namespace YoutubeDl.Wpf.Utils
{
    public static class WpfHelper
    {
        /// <summary>
        /// Opens a URL.
        /// Workaround for https://github.com/dotnet/corefx/issues/10361.
        /// </summary>
        /// <param name="url">URL to open.</param>
        public static void OpenLink(string url) => _ = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
