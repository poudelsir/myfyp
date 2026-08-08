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

    /// <summary>File extensions accepted for the mandatory Profile Photo upload — image-only (it's a selfie, not a scanned document).</summary>
    public static readonly IReadOnlyList<string> AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Largest allowed size, in megabytes, for the Government ID / Academic ID document uploads.</summary>
    public const int MaximumDocumentSizeMB = 5;

    /// <summary>
    /// File extensions accepted for the Government ID and Academic ID uploads — the same
    /// image types as <see cref="AllowedImageExtensions"/> plus PDF, since scanned
    /// identity/academic documents are frequently submitted as a PDF rather than a photo.
    /// <see cref="Services.Interfaces.IImageStorageService"/> already has magic-byte
    /// signature checks for every one of these extensions (PDF included, added for chat
    /// attachments in Phase 7.3), so no storage-service changes are needed to accept them.
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedDocumentExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

    /// <summary>MIME types accepted alongside <see cref="AllowedDocumentExtensions"/>.</summary>
    public static readonly IReadOnlyList<string> AllowedDocumentMimeTypes = new[]
    {
        "image/jpeg", "image/png", "image/webp", "application/pdf",
    };

    /// <summary>Max length of <see cref="Entities.Verification.StudentVerification.SellingCategoriesCsv"/> — comma-separated int values of <see cref="Data.Enums.SellingCategory"/>, generous enough for every category to be selected at once.</summary>
    public const int MaximumSellingCategoriesCsvLength = 200;

    /// <summary>Subfolder under the private upload root that <see cref="Services.Interfaces.IImageStorageService"/> saves verification documents into (Government ID, Academic ID, and Profile Photo all share this subfolder — filenames are GUID-based, so there's no collision risk).</summary>
    public const string ImageStorageSubfolder = "verifications";
}
