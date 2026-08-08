using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Chat;

namespace SajhaSikshya.Data.Configurations.Chat;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        // Deliberately NO filter on the message's OWN IsDeleted, unlike every other
        // entity in this project (see Conversation, Order, StudentVerification, etc.). A
        // deleted message must still occupy its place in the thread — rendered as a
        // "message deleted" placeholder by ChatMappings.ToDto — so the conversation's
        // flow isn't silently broken and the other party doesn't see messages vanish
        // mid-read. ChatService and ChatQueryService filter `!IsDeleted` explicitly
        // wherever a deleted message truly should be excluded (e.g. a conversation
        // list's "last message" preview).
        //
        // This DOES filter on the PARENT Conversation's IsDeleted — a different concern:
        // Message.Conversation is a required navigation, and EF Core warns if the
        // principal side (Conversation) has a query filter with no matching filter on
        // the dependent (Message), since a soft-deleted conversation's messages would
        // otherwise still resolve in an unfiltered query with an un-loadable required
        // navigation. Nothing sets Conversation.IsDeleted in this phase (no delete-
        // conversation feature exists yet), so this is inert today but structurally
        // correct for when one does.
        builder.HasQueryFilter(m => !m.Conversation.IsDeleted);

        builder.Property(m => m.SenderId).IsRequired();
        builder.Property(m => m.Text).HasMaxLength(ChatConstants.MaximumMessageLength);
        builder.Property(m => m.AttachmentPath).HasMaxLength(300);
        builder.Property(m => m.OriginalFileName).HasMaxLength(255);
        builder.Property(m => m.StoredFileName).HasMaxLength(255);
        builder.Property(m => m.ContentType).HasMaxLength(100);
        builder.Property(m => m.AttachmentHash).HasMaxLength(64);

        // Covers both "all messages in a conversation" and "newest message in a
        // conversation" (ChatQueryService's per-conversation latest-message lookup) without
        // a table scan.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAtUtc })
            .HasDatabaseName("IX_Messages_ConversationId_CreatedAtUtc");

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
