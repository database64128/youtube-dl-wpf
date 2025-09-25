using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Controls;
using Windows.Win32;

namespace YoutubeDl.Wpf.Utils;

public static class WpfHelper
{
    /// <summary>
    /// Opens a URI.
    /// For more information see https://github.com/dotnet/runtime/issues/17938.
    /// </summary>
    /// <param name="uri">The URI to open.</param>
    public static void OpenUri(string uri) => _ = Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

    /// <summary>
    /// Displays a file selection dialog to the user, allowing them to browse for and select a file.
    /// </summary>
    /// <remarks>If the <paramref name="path"/> parameter specifies a non-existent directory, the dialog will
    /// default to the current working directory. If the <paramref name="path"/> specifies a non-existent volume, the
    /// dialog will retry with no initial directory set.</remarks>
    /// <param name="path">The initial file path to display in the dialog. If the file name or directory is invalid, defaults will be used.</param>
    /// <param name="newPath">When the method returns <see langword="true"/>, contains the full path of the file selected by the user. When
    /// the method returns <see langword="false"/>, this parameter is set to <see langword="null"/>.</param>
    /// <param name="defaultFileName">The default file name to display if the <paramref name="path"/> does not include a valid file name. Defaults to
    /// an empty string.</param>
    /// <param name="defaultExt">The default file extension to use if the user does not specify one. Defaults to an empty string.</param>
    /// <param name="filter">The file type filter string to display in the dialog (e.g., "Text files (*.txt)|*.txt"). Defaults to an empty
    /// string.</param>
    /// <returns><see langword="true"/> if the user selects a file and confirms the dialog; otherwise, <see langword="false"/>.</returns>
    public static bool BrowseFile(
        string path,
        [NotNullWhen(true)] out string? newPath,
        string defaultFileName = "",
        string defaultExt = "",
        string filter = "")
    {
        string? fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = defaultFileName;
        }

        string? initialDirectory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(initialDirectory))
        {
            // Without an explicit initial directory, the dialog starts in
            // the last used directory, which may not work with relative paths.
            initialDirectory = Directory.GetCurrentDirectory();
        }

        Microsoft.Win32.OpenFileDialog openFileDialog = new()
        {
            FileName = fileName,
            DefaultExt = defaultExt,
            Filter = filter,
            InitialDirectory = initialDirectory,
        };

        bool? result;
        try
        {
            result = openFileDialog.ShowDialog();
        }
        catch (Win32Exception)
        {
            // ShowDialog silently ignores InitialDirectory when the path points to a non-existent directory on an existing volume.
            // But it throws a System.ComponentModel.Win32Exception when the path points to a non-existent volume.
            // So we catch the exception and try again with an empty InitialDirectory.
            openFileDialog.InitialDirectory = "";
            result = openFileDialog.ShowDialog();
        }

        if (result is not true)
        {
            newPath = null;
            return false;
        }
        newPath = openFileDialog.FileName;
        return true;
    }

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
    /// </summary>
    /// <remarks>
    /// This used to be based on https://stackoverflow.com/questions/14902394/get-the-scroll-position-of-a-wpf-textbox,
    /// but it stopped working sometime after v1.12.2, because the calculated value became a bit off.
    /// We now use a pre-calculated magic number. This needs to be adjusted if the font changes.
    /// </remarks>
    /// <param name="textBox">The TextBox.</param>
    /// <returns>
    /// <see langword="true"/> if the TextBox is scrolled to the end; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsScrolledToEnd(TextBox textBox) => textBox.VerticalOffset > textBox.ExtentHeight - textBox.ViewportHeight - 17.15;
}
