namespace SajhaSikshya.Extensions;

/// <summary>General-purpose date/time display helpers, starting with the Notification Center's relative timestamps ("5 minutes ago") — reusable anywhere else in the app that wants the same treatment later.</summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// A short, human-friendly relative description of how long ago <paramref name="utcValue"/>
    /// was, e.g. "Just now", "5 minutes ago", "3 hours ago", "Yesterday", "4 days ago".
    /// Falls back to an absolute date once it's far enough in the past that "N weeks
    /// ago" stops being more useful than just the date.
    /// </summary>
    public static string ToRelativeTimeString(this DateTime utcValue)
    {
        var elapsed = DateTime.UtcNow - utcValue;

        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (elapsed.TotalSeconds < 45)
        {
            return "Just now";
        }

        if (elapsed.TotalMinutes < 60)
        {
            var minutes = (int)elapsed.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        if (elapsed.TotalHours < 24)
        {
            var hours = (int)elapsed.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }

        if (elapsed.TotalDays < 2)
        {
            return "Yesterday";
        }

        if (elapsed.TotalDays < 7)
        {
            var days = (int)elapsed.TotalDays;
            return $"{days} days ago";
        }

        if (elapsed.TotalDays < 30)
        {
            var weeks = (int)(elapsed.TotalDays / 7);
            return $"{weeks} week{(weeks == 1 ? "" : "s")} ago";
        }

        return utcValue.ToLocalTime().ToString("MMM d, yyyy");
    }
}
