using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Data.Entities.Marketplace;

namespace SajhaSikshya.Data.Entities.Chat;

/// <summary>
/// A one-to-one thread between a listing's prospective buyer and its seller. At most
/// one non-deleted row exists per (BuyerId, SellerId, ListingId) triple — see the
/// filtered unique index in <see cref="Configurations.Chat.ConversationConfiguration"/>,
/// the same pattern <c>Order</c> uses for "one active order per listing" — so a buyer
/// re-opening the same listing's chat always lands back in the same conversation
/// rather than spawning duplicates. <see cref="BaseEntity.CreatedAtUtc"/> already
/// satisfies the spec's "Conversation.CreatedAtUtc" field; nothing extra is needed
/// for it here.
/// </summary>
public class Conversation : BaseEntity
{
    [Required]
    public string BuyerId { get; set; } = string.Empty;

    public ApplicationUser Buyer { get; set; } = null!;

    [Required]
    public string SellerId { get; set; } = string.Empty;

    public ApplicationUser Seller { get; set; } = null!;

    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;

    /// <summary>True if the buyer has hidden this conversation from their own inbox. Independent of <see cref="ArchivedBySeller"/> — each side can archive without affecting the other.</summary>
    public bool ArchivedByBuyer { get; set; }

    public bool ArchivedBySeller { get; set; }

    /// <summary>Denormalized from the most recent <see cref="Message"/> for cheap "newest conversation first" ordering without a join. Null until the first message is sent.</summary>
    public DateTime? LastMessageAtUtc { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
