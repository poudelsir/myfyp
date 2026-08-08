using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Catalog;

namespace SajhaSikshya.Data.Configurations.Catalog;

public class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.ToTable("Universities");

        // Defense in depth: even a raw EF query (e.g. via Include navigation) that
        // bypasses the repository's own IsDeleted filter still won't see soft-deleted rows.
        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Code).IsRequired().HasMaxLength(20);
        builder.Property(u => u.City).HasMaxLength(100);

        // Filtered so a soft-deleted university's code can be reused by a new one.
        builder.HasIndex(u => u.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
