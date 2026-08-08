namespace SajhaSikshya.Data.Entities.Marketplace;

/// <summary>
/// One listing in an authenticated user's persisted comparison list. Guests compare
/// entirely via session (see <see cref="Extensions.SessionExtensions"/>) and never
/// create rows here — <see cref="UserId"/> is nullable only because the spec asked for
/// that flexibility against a future need (e.g. a device/session-linked comparison),
/// not because this milestone's code path ever writes a null one. Same
/// restore-in-place toggle pattern as <see cref="SavedListing"/> — see
/// <see cref="Services.Marketplace.CompareService.AddAsync"/> — for the same reason:
/// keeping the filtered unique index satisfiable across repeated add/remove/add cycles.
/// </summary>
public class CompareListing : BaseEntity
{
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public int ListingId { get; set; }

    public Listing Listing { get; set; } = null!;
}
