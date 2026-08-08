namespace SajhaSikshya.DTOs.Verification;

/// <summary>
/// The bare minimum needed to authorize and serve a verification document —
/// deliberately not part of <see cref="VerificationDto"/>/<see cref="VerificationDetailDto"/>,
/// which are handed to Razor views and must never carry a raw storage path into
/// rendered HTML. Only <see cref="Controllers.VerificationImagesController"/> ever sees this.
/// Carries all three possible documents (Government ID is always present; Academic ID
/// and Profile Photo may be null — Academic ID because it's optional, Profile Photo
/// only for legacy rows submitted before it existed) so the controller can resolve
/// whichever one the requested route segment asks for.
/// </summary>
public class VerificationImageAccessDto
{
    public string UserId { get; set; } = string.Empty;

    public string GovernmentIdImagePath { get; set; } = string.Empty;

    public string? AcademicIdImagePath { get; set; }

    public string? ProfilePhotoImagePath { get; set; }
}
