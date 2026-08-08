using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Data.Entities.AI;

/// <summary>
/// One row per AI call attempt (Gemini call OR a cache hit OR a rejected-before-Gemini
/// validation failure) — an append-only audit trail, never updated after being written.
/// Feeds Phase 10.4's Admin AI Insights (usage-by-feature, success rate, cache
/// effectiveness); nothing outside <see cref="Services.Interfaces.AI.IAIService"/>'s
/// implementation writes to this table, mirroring how only
/// <c>NotificationService.CreateIfEnabledAsync</c> is the single write path for
/// notifications. <see cref="BaseEntity.CreatedAtUtc"/> doubles as the log's own
/// "Timestamp" field, so nothing extra is declared for it.
/// </summary>
public class AIUsageLog : BaseEntity
{
    /// <summary>Nullable — Phase 10's features are all triggered by a signed-in user today, but a future non-interactive caller (e.g. a scheduled Admin Insights refresh) shouldn't require a schema change.</summary>
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public AIFeature Feature { get; set; }

    /// <summary>Which prompt builder produced the prompt (e.g. "ListingDescription") — a plain string rather than an enum since prompt builders are expected to proliferate faster than the small, fixed <see cref="AIFeature"/> set.</summary>
    public string PromptType { get; set; } = string.Empty;

    public bool Success { get; set; }

    /// <summary>Truncated failure reason for triage — never a raw exception or stack trace. Null when <see cref="Success"/> is true.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gemini's reported total token count for this call. Null on failure, on a cache hit (nothing was actually generated), or if the API omitted usage metadata.</summary>
    public int? TokenCount { get; set; }

    public int ResponseTimeMs { get; set; }

    /// <summary>True when this attempt was served from <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> instead of calling Gemini.</summary>
    public bool FromCache { get; set; }
}
