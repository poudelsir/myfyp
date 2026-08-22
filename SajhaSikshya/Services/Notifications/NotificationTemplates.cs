namespace SajhaSikshya.Services.Notifications;

/// <summary>
/// Centralizes notification title/message text generation so wording lives in one
/// place instead of being scattered as inline string literals across every business
/// service that creates a notification. Each method returns the (Title, Message) pair
/// for exactly one triggering event — callers pass the result straight to
/// <see cref="Interfaces.Notifications.INotificationService.CreateAsync"/>. Grown
/// incrementally: <c>ChatService</c> (Phase 8.1), <c>OrderService</c>,
/// <c>VerificationService</c>, and <c>ListingService</c> (Phase 8.2/8.3) each use only
/// the templates their own integration actually raises.
/// </summary>
public static class NotificationTemplates
{
    private const int MessagePreviewLength = 100;

    public static (string Title, string Message) NewChatMessage(string senderName, string messagePreview)
    {
        var trimmed = messagePreview.Length > MessagePreviewLength
            ? messagePreview[..MessagePreviewLength] + "..."
            : messagePreview;

        return ("New message", $"{senderName}: {trimmed}");
    }

    public static (string Title, string Message) OrderCreated(string listingTitle) =>
        ("New order request", $"You have a new order request for \"{listingTitle}\".");

    public static (string Title, string Message) OrderConfirmed(string listingTitle) =>
        ("Order confirmed", $"Your order for \"{listingTitle}\" has been confirmed by the seller.");

    public static (string Title, string Message) OrderReadyForPickup(string listingTitle) =>
        ("Ready for pickup", $"\"{listingTitle}\" is ready for pickup.");

    public static (string Title, string Message) OrderCompleted(string listingTitle) =>
        ("Order completed", $"The order for \"{listingTitle}\" has been completed.");

    public static (string Title, string Message) OrderCancelled(string listingTitle, string reason) =>
        ("Order cancelled", $"The order for \"{listingTitle}\" was cancelled. Reason: {reason}");

    public static (string Title, string Message) PaymentReceived(string listingTitle, string paymentMethodDisplay) =>
        ("Payment received", $"The buyer paid online via {paymentMethodDisplay} for \"{listingTitle}\".");

    public static (string Title, string Message) VerificationSubmitted() =>
        ("Verification submitted", "Your student verification request has been submitted and is pending review.");

    public static (string Title, string Message) NewVerificationRequest(string studentName) =>
        ("New verification request", $"{studentName} submitted a new student verification request for review.");

    public static (string Title, string Message) VerificationApproved() =>
        ("Verification approved", "Congratulations! Your student verification has been approved.");

    public static (string Title, string Message) VerificationRejected(string reason) =>
        ("Verification rejected", $"Your verification request was rejected. Reason: {reason}");

    public static (string Title, string Message) ResubmissionRequested(string reason) =>
        ("Resubmission requested", $"Please resubmit your verification. Reason: {reason}");

    public static (string Title, string Message) ListingApproved(string listingTitle) =>
        ("Listing approved", $"Your listing \"{listingTitle}\" has been approved and is now live on the marketplace.");

    public static (string Title, string Message) ListingRejected(string listingTitle, string? reason) =>
        ("Listing rejected", string.IsNullOrWhiteSpace(reason)
            ? $"Your listing \"{listingTitle}\" was rejected."
            : $"Your listing \"{listingTitle}\" was rejected. Reason: {reason}");

    public static (string Title, string Message) ListingArchived(string listingTitle) =>
        ("Listing archived", $"Your listing \"{listingTitle}\" has been archived by an administrator.");

    public static (string Title, string Message) ListingRestored(string listingTitle) =>
        ("Listing restored", $"Your listing \"{listingTitle}\" has been restored and is pending review again.");

    public static (string Title, string Message) NewReviewReceived(string reviewerName, int rating) =>
        ("New review received", $"{reviewerName} left you a {rating}-star review.");

    public static (string Title, string Message) ReviewReported(string revieweeName) =>
        ("Review reported", $"A review about {revieweeName} has been reported and needs moderation.");

    public static (string Title, string Message) ReviewRemoved(string? reason) =>
        ("Review removed", string.IsNullOrWhiteSpace(reason)
            ? "Your review was removed by an administrator."
            : $"Your review was removed by an administrator. Reason: {reason}");

    // Deliberately no SystemAnnouncement/broadcast template — a broadcast's title and
    // message ARE the admin's own free-form input (see BroadcastNotificationsController),
    // with no fixed wording to centralize. A pass-through method here would just
    // echo its arguments back, the exact "unnecessary abstraction" this class exists
    // to avoid everywhere else.
}
