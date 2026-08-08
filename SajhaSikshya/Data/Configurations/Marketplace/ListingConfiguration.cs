using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Marketplace;

namespace SajhaSikshya.Data.Configurations.Marketplace;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings");
        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.Property(l => l.Title).IsRequired().HasMaxLength(150);
        builder.Property(l => l.Slug).IsRequired().HasMaxLength(180);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(2000);
        builder.Property(l => l.ModerationReason).HasMaxLength(500);

        builder.HasIndex(l => l.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Money is mapped as an EF Core complex type: Amount/Currency become two plain
        // columns on Listings, materialized back through Money's constructor — no
        // separate table, no nullability, matching how Money behaves as a value object.
        builder.ComplexProperty(l => l.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("PriceAmount").HasPrecision(18, 2);
            price.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
        });

        // Independent lookup entities (Seller, Category, Subject, AcademicLevel,
        // University) are never cascade-deleted by a Listing pointing at them — the
        // Catalog services already block deleting a row that's still referenced.
        builder.HasOne(l => l.Seller)
            .WithMany()
            .HasForeignKey(l => l.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.LastModeratedBy)
            .WithMany()
            .HasForeignKey(l => l.LastModeratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Category)
            .WithMany()
            .HasForeignKey(l => l.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Subject)
            .WithMany()
            .HasForeignKey(l => l.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.AcademicLevel)
            .WithMany()
            .HasForeignKey(l => l.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.University)
            .WithMany()
            .HasForeignKey(l => l.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Listing <-> ListingImage is circular (Listing.ThumbnailImageId points at a
        // ListingImage, ListingImage.ListingId points back at the Listing). SQL Server
        // rejects a schema where both sides cascade ("multiple cascade paths"), so only
        // ListingImageConfiguration's Listing->Images relationship cascades; this side
        // must not — ClientSetNull just clears the FK in memory via EF, never issuing a
        // DB-level ON DELETE action, which is safe since nothing in this app deletes
        // rows outside of EF's change tracking.
        builder.HasOne(l => l.ThumbnailImage)
            .WithMany()
            .HasForeignKey(l => l.ThumbnailImageId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
