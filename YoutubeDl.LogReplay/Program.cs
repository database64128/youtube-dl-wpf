using System.Globalization;

const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";
const int TimestampLength = 30;           // "yyyy-MM-dd HH:mm:ss.fff zzz"
const int LevelBracketOpenIndex = 31;      // space then '['
const int LevelBracketCloseIndex = 35;     // ']' after 3-letter level
const int MessageStartIndex = 37;

if (args.Length < 1)
{
    Console.Error.WriteLine("Please provide the path to the log file as the first command-line argument.");
    return 1;
}

try
{
    using StreamReader reader = new(args[0]);
    // Input:
    // 2025-10-01 10:24:00.000 +00:00 [INF] Initializing to normal mode (.cctor)
    //    at xxx
    // Output:
    // Initializing to normal mode (.cctor)
    //    at xxx
    DateTimeOffset? lastTimestamp = null;
    while (true)
    {
        string? line = await reader.ReadLineAsync();
        if (line is null)
        {
            break;
        }

        if (line.Length < MessageStartIndex ||
            line[LevelBracketOpenIndex] != '[' ||
            line[LevelBracketCloseIndex] != ']' ||
            !DateTimeOffset.TryParseExact(
                line.AsSpan(0, TimestampLength),
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTimeOffset timestamp))
        {
            Console.WriteLine(line);
            continue;
        }

        if (lastTimestamp is not null)
        {
            TimeSpan delay = timestamp - lastTimestamp.Value;
            if (delay.Ticks > 0)
            {
                await Task.Delay(delay);
            }
        }
        lastTimestamp = timestamp;

        string message = line[MessageStartIndex..];
        Console.WriteLine(message);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"An error occurred: {ex}");
    return 1;
}

return 0;
