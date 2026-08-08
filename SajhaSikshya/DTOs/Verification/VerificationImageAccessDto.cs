namespace SajhaSikshya.DTOs.Verification;

/// <summary>
/// The bare minimum needed to authorize and serve a verification document —
/// deliberately not part of <see cref="VerificationDto"/>/<see cref="VerificationDetailDto"/>,
/// which are handed to Razor views and must never carry a raw storage path into
/// rendered HTML. Only <see cref="Controllers.VerificationImagesController"/> ever sees this.
/// </summary>
public class VerificationImageAccessDto
{
    public string UserId { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;
}
