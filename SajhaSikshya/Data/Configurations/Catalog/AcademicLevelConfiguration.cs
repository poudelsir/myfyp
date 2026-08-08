using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Catalog;

namespace SajhaSikshya.Data.Configurations.Catalog;

public class AcademicLevelConfiguration : IEntityTypeConfiguration<AcademicLevel>
{
    public void Configure(EntityTypeBuilder<AcademicLevel> builder)
    {
        builder.ToTable("AcademicLevels");
        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Code).IsRequired().HasMaxLength(20);

        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
