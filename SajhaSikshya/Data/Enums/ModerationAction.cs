using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// The action an admin took the last time a listing was moderated. Distinct from
/// <see cref="ListingStatus"/> — this records the verb (what the moderator did),
/// while ListingStatus records the resulting state. Stored alongside
/// <see cref="Data.Entities.Marketplace.Listing.LastModeratedByUserId"/> and
/// <see cref="Data.Entities.Marketplace.Listing.LastModeratedAtUtc"/> as a lightweight
/// "last moderation event" record, not a full history table.
/// </summary>
public enum ModerationAction
{
    [Display(Name = "Approved", Description = "Moved to Active and made publicly visible.")]
    Approve = 1,

    [Display(Name = "Rejected", Description = "Sent back to the seller during review.")]
    Reject = 2,

    [Display(Name = "Archived", Description = "Removed from active listings without being sold.")]
    Archive = 3,

    [Display(Name = "Restored", Description = "Brought back from a deleted or archived state.")]
    Restore = 4,

    [Display(Name = "Deleted", Description = "Soft-deleted by an administrator.")]
    Delete = 5,
}
