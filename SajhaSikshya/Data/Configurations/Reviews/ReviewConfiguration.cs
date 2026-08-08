using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Reviews;

namespace SajhaSikshya.Data.Configurations.Reviews;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.Property(r => r.ReviewerId).IsRequired();
        builder.Property(r => r.RevieweeId).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(ReviewConstants.MaximumTitleLength);
        builder.Property(r => r.Comment).HasMaxLength(ReviewConstants.MaximumCommentLength);

        // DB-level defense-in-depth for "one review per direction per order" — the same
        // filtered-unique-index pattern as Order's "one active order per listing" and
        // Conversation's "one conversation per (buyer, seller, listing)". Non-deleted
        // rows only, so a removed-then-un-reviewed order could in principle be reviewed
        // again — reviews aren't restored the way a wrongly-moderated Listing is, so
        // this is intentionally the same shape as those precedents rather than a new one.
        builder.HasIndex(r => new { r.OrderId, r.ReviewType })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Reviews_OrderId_ReviewType");

        // Covers both "reviews I wrote" (My Reviews) and "reviews about me" (Seller/Buyer
        // Reviews, reputation aggregates) without a table scan.
        builder.HasIndex(r => r.ReviewerId).HasDatabaseName("IX_Reviews_ReviewerId");
        builder.HasIndex(r => new { r.RevieweeId, r.ReviewType }).HasDatabaseName("IX_Reviews_RevieweeId_ReviewType");

        // Admin moderation queue filters/sorts by report count.
        builder.HasIndex(r => r.ReportCount).HasDatabaseName("IX_Reviews_ReportCount");

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Reviewee)
            .WithMany()
            .HasForeignKey(r => r.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
