using System.Runtime.InteropServices;

namespace YoutubeDl.Wpf.Utils;

/// <summary>
/// Helper class for sending Ctrl + C on Windows.
/// .NET lacks the ability to send CTRL_C_EVENT on Windows, so we had to use Win32 API directly.
/// Related issue: https://github.com/dotnet/runtime/issues/14628
/// Reference: https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
/// </summary>
internal static partial class CtrlCHelper
{
    internal const int CTRL_C_EVENT = 0;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AttachConsole(uint dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FreeConsole();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, [MarshalAs(UnmanagedType.Bool)] bool Add);

    internal delegate bool ConsoleCtrlDelegate(uint dwCtrlType);
}
