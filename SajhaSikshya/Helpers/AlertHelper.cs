namespace SajhaSikshya.Helpers;

/// <summary>
/// Well-known TempData keys and Bootstrap alert-class mapping for one-shot,
/// post-redirect-get user feedback messages ("Saved successfully", "Login failed", ...).
/// Centralizing the keys avoids typo-prone magic strings scattered across controllers.
/// </summary>
public static class AlertHelper
{
    public const string SuccessKey = "AlertSuccess";
    public const string ErrorKey = "AlertError";
    public const string WarningKey = "AlertWarning";
    public const string InfoKey = "AlertInfo";

    private static readonly IReadOnlyDictionary<string, string> BootstrapClassByKey = new Dictionary<string, string>
    {
        [SuccessKey] = "alert-success",
        [ErrorKey] = "alert-danger",
        [WarningKey] = "alert-warning",
        [InfoKey] = "alert-info",
    };

    public static string BootstrapClassFor(string tempDataKey) =>
        BootstrapClassByKey.TryGetValue(tempDataKey, out var cssClass) ? cssClass : "alert-secondary";

    public static IReadOnlyList<string> AllKeys => BootstrapClassByKey.Keys.ToList();
}
