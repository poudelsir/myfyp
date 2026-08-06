namespace SajhaSikshya.ViewModels.Shared;

/// <summary>
/// Data displayed on the generic error page. RequestId is surfaced so a user can
/// report it to support and it can be correlated against server-side logs.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>Optional human-readable message for known error scenarios (e.g. 404).</summary>
    public string? Message { get; set; }
}
