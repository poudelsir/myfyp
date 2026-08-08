namespace SajhaSikshya.Constants;

/// <summary>Business-rule limits for the Student Verification system (Phase 5), mirroring how <see cref="ListingConstants"/> centralizes the Listing module's limits.</summary>
public static class VerificationConstants
{
    public const int MinimumStudentNumberLength = 3;

    public const int MaximumStudentNumberLength = 50;

    public const int MaximumRejectionReasonLength = 500;

    public const int MaximumAdminNotesLength = 1000;

    /// <summary>Largest allowed size, in megabytes, for a student ID upload.</summary>
    public const int MaximumImageSizeMB = 5;

    /// <summary>File extensions accepted for a student ID upload — same set as listing photos (<see cref="ListingConstants.AllowedImageExtensions"/>), kept as its own list since the two are conceptually unrelated and shouldn't change in lockstep just because they happen to match today.</summary>
    public static readonly IReadOnlyList<string> AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Subfolder under <c>wwwroot/uploads/</c> that <see cref="Services.Interfaces.IImageStorageService"/> saves student ID uploads into.</summary>
    public const string ImageStorageSubfolder = "verifications";
}
