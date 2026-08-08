using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Marketplace;

namespace SajhaSikshya.Data.Configurations.Marketplace;

public class SavedListingConfiguration : IEntityTypeConfiguration<SavedListing>
{
    public void Configure(EntityTypeBuilder<SavedListing> builder)
    {
        builder.ToTable("SavedListings");
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.Property(s => s.UserId).IsRequired();

        // One active save per user per listing. Filtered to non-deleted rows (same
        // pattern as Listing.Slug's unique index) so toggling save -> unsave -> save
        // again never collides with the row left behind by the first save — it's
        // restored in place instead of a second row being inserted; see
        // SavedListingService.ToggleSaveAsync.
        builder.HasIndex(s => new { s.UserId, s.ListingId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Independent entities (User, Listing) are never cascade-deleted by a
        // SavedListing pointing at them — same rationale as Listing's own FKs to
        // Seller/Category/etc. Users and Listings are soft-deleted, never removed, so a
        // cascade path would never actually fire in practice, but Restrict keeps the
        // intent explicit rather than relying on that.
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Listing)
            .WithMany()
            .HasForeignKey(s => s.ListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
