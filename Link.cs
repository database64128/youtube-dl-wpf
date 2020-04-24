using System.Diagnostics;
using System.Runtime.InteropServices;

namespace youtube_dl_wpf
{
    public static class Link
    {
        public static void OpenInBrowser(string url)
        {
            //https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }
    }
}
