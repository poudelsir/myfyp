namespace SajhaSikshya.Constants;

/// <summary>Business-rule limits for the Profile module's Personal Information section.</summary>
public static class ProfileConstants
{
    /// <summary>File extensions accepted for a profile photo upload — image-only, mirrors <see cref="VerificationConstants.AllowedImageExtensions"/>.</summary>
    public static readonly IReadOnlyList<string> AllowedPhotoExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Largest allowed size, in megabytes, for a profile photo upload.</summary>
    public const int MaximumPhotoSizeMB = 5;

    /// <summary>
    /// Subfolder under the public upload root that a profile photo is saved into — the
    /// same subfolder <see cref="Services.Verification.VerificationService"/> already
    /// writes an approved verification Profile Photo into on approval, so both paths
    /// produce interchangeable, ordinary public "uploads/profiles/{guid}.ext" URLs.
    /// </summary>
    public const string PhotoStorageSubfolder = "profiles";
}
