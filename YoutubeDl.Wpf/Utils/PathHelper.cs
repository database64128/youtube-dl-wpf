using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Windows.Win32;

namespace YoutubeDl.Wpf.Utils;

public static class PathHelper
{
    /// <summary>
    /// Determines whether a file exists at the specified path, searching the system's search path if the path is not
    /// fully qualified.
    /// </summary>
    /// <remarks>If the <paramref name="path"/> is fully qualified, the method directly checks for the file's
    /// existence at the specified location. If the path is not fully qualified, the system's search path is used to
    /// locate the file.</remarks>
    /// <param name="path">The file path to check. If the path is not fully qualified, the system's search path is used
    /// to locate the file.</param>
    /// <returns><see langword="true"/> if the file exists at the specified path or is found in the system's search path;
    /// otherwise, <see langword="false"/>.</returns>
    public static unsafe bool FileExistsSearchPath(string path) => Path.IsPathFullyQualified(path) ?
        File.Exists(path) :
        PInvoke.SearchPath(default, path, default, default, default) > 0;

    /// <summary>
    /// Searches for the specified file in the system's search path and determines its fully qualified path.
    /// </summary>
    /// <remarks>If the <paramref name="path"/> is already a fully qualified path, the method checks if the
    /// file exists at the specified location. If the file exists, <paramref name="fullPath"/> is set to the input
    /// path. If the <paramref name="path"/> is not fully qualified, the method searches for the file in the system's 
    /// search path. The search uses the system's default behavior, which includes searching in the current directory
    /// and directories specified in the PATH environment variable. The method uses a fixed buffer size of 32,767
    /// characters for the search result. If the resulting path exceeds this length, the method fails and returns <see
    /// langword="false"/>.</remarks>
    /// <param name="path">The name of the file to search for.</param>
    /// <param name="fullPath">When the method returns <see langword="true"/>, contains the fully qualified path of the
    /// file; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the file is found and its fully qualified path is determined; otherwise, <see
    /// langword="false"/>.</returns>
    public static unsafe bool SearchPath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        if (Path.IsPathFullyQualified(path))
        {
            if (File.Exists(path))
            {
                fullPath = path;
                return true;
            }
            fullPath = null;
            return false;
        }

        Span<char> lpBuffer = stackalloc char[32767];
        uint length = PInvoke.SearchPath(default, path, default, lpBuffer, default);
        if (length == 0 || length > lpBuffer.Length)
        {
            fullPath = null;
            return false;
        }
        fullPath = lpBuffer[..(int)length].ToString();
        return true;
    }
}
