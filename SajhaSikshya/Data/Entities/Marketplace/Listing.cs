using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Data.ValueObjects;

namespace SajhaSikshya.Data.Entities.Marketplace;

/// <summary>
/// A single item a seller has listed on the marketplace (secondhand book or study
/// material). Inherits <see cref="BaseEntity"/> directly rather than
/// <see cref="Catalog.BaseLookupEntity"/> — it isn't a simple Name/IsActive reference
/// value, it's a rich transactional entity with its own lifecycle
/// (see <see cref="ListingStatus"/>) that BaseLookupEntity's shape doesn't fit.
/// </summary>
public class Listing : BaseEntity
{
    [Required]
    [StringLength(ListingConstants.MaximumTitleLength, MinimumLength = ListingConstants.MinimumTitleLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>URL-safe identifier generated from <see cref="Title"/> via <see cref="Data.ValueObjects.Slug"/>. Immutable after creation — editing the title does not change the URL. Unique among non-deleted listings.</summary>
    [Required]
    [StringLength(180)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [StringLength(ListingConstants.MaximumDescriptionLength)]
    public string Description { get; set; } = string.Empty;

    public Money Price { get; set; } = Money.Zero();

    /// <summary>True if this item is being given away rather than sold; <see cref="Price"/> is expected to be zero when true.</summary>
    public bool IsDonation { get; set; }

    public BookCondition Condition { get; set; }

    public ListingStatus Status { get; set; } = ListingStatus.Draft;

    [Required]
    public string SellerId { get; set; } = string.Empty;

    public ApplicationUser Seller { get; set; } = null!;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    /// <summary>Denormalized from <see cref="Subject"/> at save time for fast filtering without a join; kept consistent by <see cref="Services.Marketplace.ListingService"/>, not independently chosen by the seller.</summary>
    public int AcademicLevelId { get; set; }

    public AcademicLevel AcademicLevel { get; set; } = null!;

    /// <summary>Denormalized from <see cref="Subject"/>, same rationale as <see cref="AcademicLevelId"/>. Nullable because <see cref="Catalog.Subject.UniversityId"/> is optional.</summary>
    public int? UniversityId { get; set; }

    public University? University { get; set; }

    public int? ThumbnailImageId { get; set; }

    public ListingImage? ThumbnailImage { get; set; }

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();

    public int ViewCount { get; set; }

    public int FavoriteCount { get; set; }

    /// <summary>
    /// The verb behind the most recent admin moderation event (Approve/Reject/Archive/
    /// Restore/Delete), alongside <see cref="LastModeratedByUserId"/>,
    /// <see cref="LastModeratedAtUtc"/>, and <see cref="ModerationReason"/>. Null until a
    /// listing is first moderated. This tracks only the latest event, not a full history —
    /// a dedicated audit-log table is the right home for that if it's ever needed.
    /// </summary>
    public ModerationAction? LastModerationAction { get; set; }

    public string? LastModeratedByUserId { get; set; }

    public ApplicationUser? LastModeratedBy { get; set; }

    public DateTime? LastModeratedAtUtc { get; set; }

    /// <summary>Optional moderator note, most useful on Reject to tell the seller what to fix.</summary>
    [StringLength(500)]
    public string? ModerationReason { get; set; }
}
