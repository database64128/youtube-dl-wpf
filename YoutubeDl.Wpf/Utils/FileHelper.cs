using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDl.Wpf.Utils;

public static class FileHelper
{
    /// <summary>
    /// Loads the specified JSON file and deserializes its content as a <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
    /// <param name="path">JSON file path.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the read operation.</param>
    /// <returns>A <typeparamref name="TValue"/>.</returns>
    public static async Task<TValue> LoadFromJsonFileAsync<TValue>(string path, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default) where TValue : class, new()
    {
        if (!File.Exists(path))
            return new();

        var fileStream = new FileStream(path, FileMode.Open);
        await using (fileStream.ConfigureAwait(false))
        {
            return await JsonSerializer.DeserializeAsync(fileStream, jsonTypeInfo, cancellationToken).ConfigureAwait(false) ?? new();
        }
    }

    /// <summary>
    /// Serializes the provided value as JSON and saves to the specified file.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="path">JSON file path.</param>
    /// <param name="value">The value to save.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task SaveToJsonFileAsync<TValue>(
        string path,
        TValue value,
        JsonTypeInfo<TValue> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        // File.Replace throws an exception when the destination file does not exist.
        var canReplace = File.Exists(path);
        var newPath = canReplace ? $"{path}.new" : path;
        var fileStream = new FileStream(newPath, FileMode.Create);

        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, value, jsonTypeInfo, cancellationToken);
        }

        if (canReplace)
            File.Replace(newPath, path, $"{path}.old");
    }
}
