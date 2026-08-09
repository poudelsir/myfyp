using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Lifecycle state of a marketplace listing, from creation through to its final
/// outcome. Drives which actions (edit, approve, reserve, mark sold) are available
/// on a listing at any given point, and which statuses are visible to buyers versus
/// only to the seller/admin.
/// </summary>
public enum ListingStatus
{
    /// <summary>Saved by the seller but not yet submitted for review.</summary>
    [Display(Name = "Draft", Description = "Saved by the seller but not yet submitted for review.")]
    Draft = 0,

    /// <summary>Submitted by the seller and awaiting admin approval before going live.</summary>
    [Display(Name = "Pending Approval", Description = "Submitted by the seller and awaiting admin approval.")]
    PendingApproval = 1,

    /// <summary>Approved and visible to buyers in the marketplace.</summary>
    [Display(Name = "Active", Description = "Approved and visible to buyers in the marketplace.")]
    Active = 2,

    /// <summary>A buyer has reserved this item; it is temporarily held pending completion.</summary>
    [Display(Name = "Reserved", Description = "Reserved by a buyer and temporarily held.")]
    Reserved = 3,

    /// <summary>The item has been sold and is no longer available.</summary>
    [Display(Name = "Sold", Description = "Sold and no longer available.")]
    Sold = 4,

    /// <summary>The item was given away rather than sold.</summary>
    [Display(Name = "Donated", Description = "Given away rather than sold.")]
    Donated = 5,

    /// <summary>Removed from active listings by the seller or an admin, without being sold.</summary>
    [Display(Name = "Archived", Description = "Removed from active listings without being sold.")]
    Archived = 6,

    /// <summary>Rejected during admin review and sent back to the seller with feedback.</summary>
    [Display(Name = "Rejected", Description = "Rejected during admin review.")]
    Rejected = 7,

    /// <summary>Stock reached zero; hidden from all public marketplace/search/AI surfaces but stays visible to the seller and admin. Auto-restored to Active when the seller or admin sets a new stock quantity above zero.</summary>
    [Display(Name = "Out of Stock", Description = "Stock reached zero; hidden from the marketplace until restocked.")]
    OutOfStock = 8,
}
