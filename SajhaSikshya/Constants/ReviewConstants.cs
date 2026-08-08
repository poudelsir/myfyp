namespace SajhaSikshya.Constants;

/// <summary>Business-rule limits for the Review module (Phase 9), mirroring how <see cref="ChatConstants"/>/<see cref="OrderConstants"/> centralize their own modules' limits.</summary>
public static class ReviewConstants
{
    public const int MinimumRating = 1;

    public const int MaximumRating = 5;

    public const int MaximumTitleLength = 150;

    public const int MaximumCommentLength = 1000;

    /// <summary>How long after posting a review its author may still edit it — longer than Chat's 15-minute window (<see cref="ChatConstants.MessageEditWindowMinutes"/>) since a review is a considered, one-time judgment rather than a live conversation, and typos are more consequential on something publicly visible for a long time.</summary>
    public const int EditWindowHours = 48;
}
