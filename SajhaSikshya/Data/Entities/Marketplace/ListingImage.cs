using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Entities.Marketplace;

/// <summary>
/// One uploaded photo belonging to a <see cref="Listing"/>. Soft-deletable like every
/// other entity for row-level audit consistency, but the physical file on disk is
/// removed immediately when an image is deleted — there is no "restore image" feature,
/// so keeping orphaned files around after deletion serves no purpose.
/// </summary>
public class ListingImage : BaseEntity
{
    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;

    /// <summary>Relative path under wwwroot (e.g. "/uploads/listings/42/&lt;secure-name&gt;.jpg").</summary>
    [Required]
    [StringLength(300)]
    public string ImagePath { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    /// <summary>
    /// Denormalized convenience flag mirroring whether this image is the listing's
    /// <see cref="Listing.ThumbnailImageId"/>. The FK on Listing is authoritative; this
    /// is kept in sync by the service layer so code holding only a ListingImage (no
    /// join back to Listing) can still tell which one is the thumbnail.
    /// </summary>
    public bool IsThumbnail { get; set; }
}
