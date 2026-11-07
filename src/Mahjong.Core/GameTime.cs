using System.Globalization;

namespace Mahjong.Core;

public static class GameTime
{
    // A generous, exactly representable bound also leaves room for timer increments.
    public const double MaxSeconds = 100 * 365 * 24 * 60 * 60d;
    public static bool Valid(double seconds) => double.IsFinite(seconds) && seconds >= 0 && seconds <= MaxSeconds;
    public static bool ValidDate(string? value) => value == null || DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    public static string Format(double seconds)
    {
        if (!Valid(seconds)) return "--:--:--";
        var time = TimeSpan.FromSeconds(seconds);
        return string.Create(CultureInfo.InvariantCulture, $"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}");
    }
}
