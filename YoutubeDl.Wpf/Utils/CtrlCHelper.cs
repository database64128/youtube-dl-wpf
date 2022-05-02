using System.Runtime.InteropServices;

namespace YoutubeDl.Wpf.Utils;

/// <summary>
/// Helper class for sending Ctrl + C on Windows.
/// .NET lacks the ability to send CTRL_C_EVENT on Windows, so we had to use Win32 API directly.
/// Related issue: https://github.com/dotnet/runtime/issues/14628
/// Reference: https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
/// </summary>
internal static class CtrlCHelper
{
    internal const int CTRL_C_EVENT = 0;

    [DllImport("kernel32.dll")]
    internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, bool Add);

    internal delegate bool ConsoleCtrlDelegate(uint dwCtrlType);
}
