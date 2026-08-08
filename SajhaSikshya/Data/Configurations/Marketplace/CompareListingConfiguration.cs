using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Marketplace;

namespace SajhaSikshya.Data.Configurations.Marketplace;

public class CompareListingConfiguration : IEntityTypeConfiguration<CompareListing>
{
    public void Configure(EntityTypeBuilder<CompareListing> builder)
    {
        builder.ToTable("CompareListings");
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Same filtered-unique-index shape as SavedListing. Note: SQL Server treats
        // multiple NULL UserId rows as distinct for uniqueness purposes, so this would
        // not by itself prevent duplicates for a null-UserId row — a non-issue today
        // since every row this milestone creates has a real UserId (guests never reach
        // the database at all), but worth remembering if UserId's nullability is ever
        // actually used for a session/device-linked row in the future.
        builder.HasIndex(c => new { c.UserId, c.ListingId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Listing)
            .WithMany()
            .HasForeignKey(c => c.ListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
