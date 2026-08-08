using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Orders;

namespace SajhaSikshya.Data.Configurations.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.Property(o => o.BuyerId).IsRequired();
        builder.Property(o => o.SellerId).IsRequired();
        builder.Property(o => o.CreatedByUserId).IsRequired();
        builder.Property(o => o.PickupNotes).HasMaxLength(500);
        builder.Property(o => o.CancellationReason).HasMaxLength(500);
        builder.Property(o => o.ReferenceNumber).IsRequired().HasMaxLength(32);

        // Generated once at creation and never reassigned — unique so it's safe to hand
        // out on a receipt or to a future payment gateway as the row's public identifier.
        builder.HasIndex(o => o.ReferenceNumber)
            .IsUnique()
            .HasDatabaseName("IX_Orders_ReferenceNumber");

        // One active (Pending/Confirmed/ReadyForPickup — 0/1/2) order per listing at a
        // time — DB-level defense-in-depth on top of the service-layer check, same
        // filtered-unique-index pattern as StudentVerification's "one Pending per user".
        // Completed/Cancelled (3/4) are terminal and deliberately excluded from the
        // filter, so a listing's full order history can have many rows.
        builder.HasIndex(o => o.ListingId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Status] IN (0, 1, 2)")
            .HasDatabaseName("IX_Orders_ListingId_OneActive");

        // Buyer, Seller, and the Listing are never cascade-deleted by an Order pointing
        // at them — same rationale as every other FK in this app pointing at a
        // never-hard-deleted (soft-delete only) row.
        builder.HasOne(o => o.Buyer)
            .WithMany()
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Seller)
            .WithMany()
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Listing)
            .WithMany()
            .HasForeignKey(o => o.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.StatusHistory)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
