namespace SajhaSikshya.Data.Entities.Marketplace;

/// <summary>
/// A Student's personal bookmark of a <see cref="Marketplace.Listing"/> ("save for
/// later" / wishlist). Inherits <see cref="BaseEntity"/> like every other entity for
/// consistent soft-delete/audit behavior — unsaving a listing soft-deletes this row
/// rather than removing it, and re-saving the same listing later restores it (see
/// <see cref="Services.Marketplace.SavedListingService.ToggleSaveAsync"/>) instead of
/// inserting a second row, which is what keeps the one-save-per-user-per-listing
/// uniqueness constraint (see <see cref="Configurations.Marketplace.SavedListingConfiguration"/>)
/// satisfiable across repeated toggles.
/// </summary>
public class SavedListing : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;
}
