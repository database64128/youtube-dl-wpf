using System.Text.Json.Serialization;

namespace YoutubeDl.Wpf.Models;

[JsonSerializable(typeof(Settings))]
[JsonSourceGenerationOptions(
    IgnoreReadOnlyProperties = true,
    WriteIndented = true)]
public partial class SettingsJsonSerializerContext : JsonSerializerContext
{
}
