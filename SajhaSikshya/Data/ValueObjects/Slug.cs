using System.Text.RegularExpressions;
using SajhaSikshya.Utilities;

namespace SajhaSikshya.Data.ValueObjects;

/// <summary>
/// A validated, URL-safe identifier (e.g. "past-question-papers"). Normalization is
/// delegated to <see cref="SlugHelper"/> (the same helper <c>CategoryService</c> already
/// uses) rather than reimplemented here, so there is exactly one place that decides how
/// text becomes a slug.
/// </summary>
public readonly struct Slug : IEquatable<Slug>
{
    private static readonly Regex ValidSlugPattern = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    /// <summary>Normalizes arbitrary input (e.g. a listing title) into a <see cref="Slug"/> via <see cref="SlugHelper.Generate"/>.</summary>
    public static Slug FromText(string text)
    {
        var normalized = SlugHelper.Generate(text);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Could not generate a valid slug from the provided text.", nameof(text));
        }

        return new Slug(normalized);
    }

    /// <summary>Wraps an already-slugified value (e.g. one loaded back from the database), validating it's well-formed.</summary>
    public static Slug FromValue(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException($"'{value}' is not a valid slug.", nameof(value));
        }

        return new Slug(value);
    }

    /// <summary>True if <paramref name="value"/> is lowercase alphanumeric segments separated by single hyphens, with no leading/trailing/double hyphens.</summary>
    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) && ValidSlugPattern.IsMatch(value);

    /// <summary>
    /// Generates a slug from <paramref name="baseText"/> and, if it's already taken,
    /// appends a numeric suffix ("past-papers-2", "past-papers-3", ...) until
    /// <paramref name="isTaken"/> reports one that's free. Takes a delegate rather than a
    /// repository so this value object stays free of any data-access dependency; callers
    /// typically pass something like <c>slug => repository.AnyAsync(x => x.Slug == slug)</c>.
    /// </summary>
    public static async Task<Slug> MakeUniqueAsync(string baseText, Func<string, Task<bool>> isTaken)
    {
        var candidate = FromText(baseText);
        if (!await isTaken(candidate.Value))
        {
            return candidate;
        }

        var suffix = 2;
        while (true)
        {
            var candidateValue = $"{candidate.Value}-{suffix}";
            if (!await isTaken(candidateValue))
            {
                return new Slug(candidateValue);
            }

            suffix++;
        }
    }

    public bool Equals(Slug other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is Slug other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}
