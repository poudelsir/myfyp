namespace SajhaSikshya.Constants;

/// <summary>
/// Business-rule limits for the (future) marketplace Listing module. Centralizing
/// these means a rule like "titles are at most 150 characters" is asserted once and
/// referenced everywhere (ViewModel DataAnnotations, service-layer validation,
/// client-side character counters) instead of being copy-pasted as a bare number.
/// </summary>
public static class ListingConstants
{
    /// <summary>Shortest allowed listing title. Below this, titles tend to be too vague to be useful ("Book").</summary>
    public const int MinimumTitleLength = 5;

    /// <summary>Longest allowed listing title, chosen to stay readable in list/card views and search results.</summary>
    public const int MaximumTitleLength = 150;

    /// <summary>Longest allowed listing description.</summary>
    public const int MaximumDescriptionLength = 2000;

    /// <summary>Most images a single listing may have attached.</summary>
    public const int MaximumImages = 8;

    /// <summary>Largest allowed size, in megabytes, for a single listing image.</summary>
    public const int MaximumImageSizeMB = 5;

    /// <summary>
    /// File extensions accepted for listing images. Compared programmatically (e.g. via
    /// <see cref="Data.ValueObjects.ImageFile.HasAllowedExtension"/>) — DataAnnotation
    /// attributes require compile-time constant arguments, so this list can't be passed
    /// directly to an attribute like <see cref="Validators.AllowedFileExtensionsAttribute"/>.
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Lowest allowed listing price. Zero is allowed so items can be listed as free/donations.</summary>
    public const decimal MinimumPrice = 0m;

    /// <summary>
    /// Highest allowed listing price (NPR). A sanity ceiling against data-entry mistakes
    /// (e.g. an extra zero), not a real market cap — revisit if the marketplace expands
    /// beyond secondhand books/study materials.
    /// </summary>
    public const decimal MaximumPrice = 100000m;
}
