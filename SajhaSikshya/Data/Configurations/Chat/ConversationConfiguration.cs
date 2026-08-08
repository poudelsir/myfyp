using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Data.Entities.Chat;

namespace SajhaSikshya.Data.Configurations.Chat;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.BuyerId).IsRequired();
        builder.Property(c => c.SellerId).IsRequired();

        // One conversation per (buyer, seller, listing) — DB-level defense-in-depth on
        // top of ChatService.CreateConversationAsync's own reuse check, same filtered-
        // unique-index pattern as Order's "one active order per listing".
        builder.HasIndex(c => new { c.BuyerId, c.SellerId, c.ListingId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Conversations_Buyer_Seller_Listing");

        builder.HasOne(c => c.Buyer)
            .WithMany()
            .HasForeignKey(c => c.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Seller)
            .WithMany()
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Listing)
            .WithMany()
            .HasForeignKey(c => c.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
