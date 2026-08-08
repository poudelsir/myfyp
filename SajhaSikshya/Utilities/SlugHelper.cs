using System.Text;

namespace SajhaSikshya.Utilities;

/// <summary>
/// Converts free-text input into a lowercase, hyphen-separated, URL-safe slug
/// (e.g. "Past Question Papers!" -> "past-question-papers"). Used wherever an
/// entity needs a stable, human-readable identifier for URLs (e.g. Category).
/// </summary>
public static class SlugHelper
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        var lastWasHyphen = false;

        foreach (var ch in input.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.ToString();
    }
}
