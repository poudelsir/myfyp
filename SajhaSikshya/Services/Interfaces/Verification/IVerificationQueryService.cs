using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Verification;

namespace SajhaSikshya.Services.Interfaces.Verification;

/// <summary>
/// Read-only Student Verification queries, split out from <see cref="IVerificationService"/>
/// the same way <c>IListingQueryService</c> is split from <c>IListingService</c>. Every
/// method returns DTOs, never tracked entities. Student-facing reads
/// (<see cref="GetCurrentStatusAsync"/>/<see cref="GetHistoryAsync"/>) and admin-facing
/// reads (queue, statistics, single-record review) sit side by side here — same
/// convention as <c>IListingQueryService</c>, where the distinction is entirely in
/// what each method's own filter enforces, not in separate services.
/// <see cref="IsUserVerifiedAsync"/>/<see cref="GetVerifiedUserIdsAsync"/> are the one
/// canonical way to check verification status anywhere else in the app (the
/// <c>VerifiedStudent</c> authorization handler and every "Verified Student" badge
/// route through these two, rather than each re-implementing the same query).
/// </summary>
public interface IVerificationQueryService
{
    /// <summary>The user's most recent verification request (by submission date), or null if they've never submitted one. This is that user's "current" verification state — Profile Status / the Student dashboard's status card both read from this.</summary>
    Task<VerificationDto?> GetCurrentStatusAsync(string userId);

    /// <summary>Every verification request the user has ever submitted, newest first — the full audit trail Phase 5 requires be preserved, never just the latest one.</summary>
    Task<PagedResult<VerificationDto>> GetHistoryAsync(string userId, int pageNumber, int pageSize);

    /// <summary>The Admin review queue: every verification request regardless of owner, optionally narrowed by status and/or a search term matched against student name or student number.</summary>
    Task<PagedResult<VerificationDto>> GetQueueAsync(VerificationStatus? status, string? searchTerm, int pageNumber, int pageSize);

    /// <summary>Full admin detail lookup for the review page — unrestricted by owner, includes <c>AdminNotes</c> (never exposed via <see cref="VerificationDto"/>). Null if the id doesn't exist.</summary>
    Task<VerificationDetailDto?> GetForReviewAsync(int verificationId);

    /// <summary>Aggregate counts, approval rate, average review time, and the most recently reviewed requests, for the Admin Verification Dashboard.</summary>
    Task<VerificationStatisticsDto> GetStatisticsAsync();

    /// <summary>The owner id and storage path needed to authorize and serve a verification document — see <see cref="Controllers.VerificationImagesController"/>. Null if the id doesn't exist.</summary>
    Task<VerificationImageAccessDto?> GetImageAccessAsync(int verificationId);

    /// <summary>Whether <paramref name="userId"/> currently has an approved (Verified) request. The one canonical verification check — see this interface's remarks.</summary>
    Task<bool> IsUserVerifiedAsync(string userId);

    /// <summary>Out of <paramref name="userIds"/>, the subset that are currently Verified — one batched query, used to stamp the "Verified Student" badge across a whole page of listing cards/sellers without checking each one individually.</summary>
    Task<IReadOnlySet<string>> GetVerifiedUserIdsAsync(IEnumerable<string> userIds);
}
