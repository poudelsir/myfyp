using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Catalog;

namespace SajhaSikshya.Data.Configurations.Catalog;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(120);

        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Self-referencing FK: SQL Server disallows Cascade here (multiple cascade
        // paths/cycles), so a parent with subcategories must be deleted explicitly.
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.Subcategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
