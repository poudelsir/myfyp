using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.AI;

namespace SajhaSikshya.Data.Configurations.AI;

public class AIUsageLogConfiguration : IEntityTypeConfiguration<AIUsageLog>
{
    public void Configure(EntityTypeBuilder<AIUsageLog> builder)
    {
        builder.ToTable("AIUsageLogs");
        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.Property(l => l.PromptType).IsRequired().HasMaxLength(100);
        builder.Property(l => l.ErrorMessage).HasMaxLength(AIConstants.MaxErrorMessageLength);

        // Admin AI Insights (Phase 10.4) breaks usage down by feature and by user over time.
        builder.HasIndex(l => l.Feature).HasDatabaseName("IX_AIUsageLogs_Feature");
        builder.HasIndex(l => l.UserId).HasDatabaseName("IX_AIUsageLogs_UserId");
        builder.HasIndex(l => l.CreatedAtUtc).HasDatabaseName("IX_AIUsageLogs_CreatedAtUtc");

        // Restrict (not Cascade/SetNull): a log is an immutable audit record — it should
        // never silently lose its UserId or vanish because the user's account changed.
        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
