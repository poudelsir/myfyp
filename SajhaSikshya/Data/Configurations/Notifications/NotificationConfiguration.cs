using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Notifications;

namespace SajhaSikshya.Data.Configurations.Notifications;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(NotificationConstants.MaximumTitleLength);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(NotificationConstants.MaximumMessageLength);
        builder.Property(n => n.Link).HasMaxLength(NotificationConstants.MaximumLinkLength);
        builder.Property(n => n.CreatedBy).HasMaxLength(450);

        // Covers the paginated "history" query (WHERE UserId = @userId ORDER BY
        // CreatedAtUtc DESC) — the Notification Center's and the navbar dropdown's only
        // access pattern.
        builder.HasIndex(n => new { n.UserId, n.CreatedAtUtc })
            .HasDatabaseName("IX_Notifications_UserId_CreatedAtUtc");

        // Separate covering index for the unread-count query specifically (WHERE UserId
        // = @userId AND IsRead = 0), run on nearly every page load via the navbar badge.
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
