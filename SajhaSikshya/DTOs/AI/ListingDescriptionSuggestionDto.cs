namespace SajhaSikshya.DTOs.AI;

/// <summary>AI-generated suggestion returned to the Create/Edit Listing form — the seller reviews and can freely edit every field before saving; nothing here is persisted automatically.</summary>
public class ListingDescriptionSuggestionDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Informational only — <see cref="Data.Entities.Marketplace.Listing"/> has no Keywords column, so these are shown as a hint the seller can fold into their description themselves rather than being auto-saved anywhere.</summary>
    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();
}
