namespace SajhaSikshya.Services.AI.Prompts;

/// <summary>
/// Everything <see cref="MarketplaceAssistantPromptBuilder"/> needs — live marketplace
/// facts (resolved by <see cref="IMarketplaceAssistantService"/> from the existing
/// Listing/Catalog/Verification query services, never hardcoded here) plus the caller's
/// question and recent history. <see cref="IsVerified"/> and <see cref="Role"/> are the
/// only per-user facts included — deliberately not order/listing counts, so the prompt
/// (and its cache key) stays scoped to "what changes the correct answer" rather than
/// fragmenting the cache per user.
/// </summary>
public record MarketplaceAssistantPromptInput(
    string Question,
    IReadOnlyList<AssistantMessage> History,
    bool IsVerified,
    string Role,
    int ActiveListingCount,
    int TotalListingCount,
    int DonationListingCount,
    IReadOnlyList<string> CategoryNames,
    IReadOnlyList<string> AcademicLevelNames,
    IReadOnlyList<string> UniversityNames);
