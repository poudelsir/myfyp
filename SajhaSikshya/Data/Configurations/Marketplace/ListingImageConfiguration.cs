using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Marketplace;

namespace SajhaSikshya.Data.Configurations.Marketplace;

public class ListingImageConfiguration : IEntityTypeConfiguration<ListingImage>
{
    public void Configure(EntityTypeBuilder<ListingImage> builder)
    {
        builder.ToTable("ListingImages");
        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.Property(i => i.ImagePath).IsRequired().HasMaxLength(300);

        // This is the one side of the Listing<->ListingImage cycle allowed to cascade —
        // see the comment on Listing.ThumbnailImage in ListingConfiguration.
        builder.HasOne(i => i.Listing)
            .WithMany(l => l.Images)
            .HasForeignKey(i => i.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
