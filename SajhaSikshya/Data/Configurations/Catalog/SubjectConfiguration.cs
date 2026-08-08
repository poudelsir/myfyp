using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Catalog;

namespace SajhaSikshya.Data.Configurations.Catalog;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Code).HasMaxLength(20);

        // Restrict (not Cascade): deleting a level/university with subjects still
        // attached must be blocked, not silently take the subjects down with it.
        builder.HasOne(s => s.AcademicLevel)
            .WithMany(l => l.Subjects)
            .HasForeignKey(s => s.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.University)
            .WithMany(u => u.Subjects)
            .HasForeignKey(s => s.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
