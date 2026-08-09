using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Verification;

namespace SajhaSikshya.Data.Configurations.Verification;

public class StudentVerificationConfiguration : IEntityTypeConfiguration<StudentVerification>
{
    public void Configure(EntityTypeBuilder<StudentVerification> builder)
    {
        builder.ToTable("StudentVerifications");
        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.Property(v => v.UserId).IsRequired();
        builder.Property(v => v.StudentNumber).HasMaxLength(50);
        builder.Property(v => v.GovernmentIdImagePath).IsRequired().HasMaxLength(300);
        builder.Property(v => v.AcademicIdImagePath).HasMaxLength(300);
        builder.Property(v => v.ProfilePhotoImagePath).HasMaxLength(300);
        builder.Property(v => v.InstitutionName).HasMaxLength(150);
        builder.Property(v => v.SellingCategoriesCsv).IsRequired().HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(v => v.DeclarationAccepted).IsRequired().HasDefaultValue(false);
        builder.Property(v => v.RejectionReason).HasMaxLength(500);
        builder.Property(v => v.AdminNotes).HasMaxLength(1000);

        // Only one Pending request per user at a time — DB-level defense-in-depth on top
        // of the service-layer check in VerificationService, filtered to Pending (0) rows
        // specifically (not all rows) since a user's full history intentionally has many
        // non-deleted rows over time — same filtered-unique-index pattern as
        // SavedListing/CompareListing, just filtered on Status instead of only IsDeleted.
        builder.HasIndex(v => v.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Status] = 0")
            .HasDatabaseName("IX_StudentVerifications_UserId_OnePending");

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.University)
            .WithMany()
            .HasForeignKey(v => v.UniversityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.ReviewedByUser)
            .WithMany()
            .HasForeignKey(v => v.ReviewedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
