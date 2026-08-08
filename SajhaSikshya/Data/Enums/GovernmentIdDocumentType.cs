using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Which government-issued document a seller applicant uploaded as their mandatory
/// identity proof (<see cref="Entities.Verification.StudentVerification.GovernmentIdImagePath"/>).
/// </summary>
public enum GovernmentIdDocumentType
{
    [Display(Name = "Citizenship Certificate")]
    CitizenshipCertificate = 0,

    [Display(Name = "National ID Card")]
    NationalId = 1,

    [Display(Name = "Passport")]
    Passport = 2,

    [Display(Name = "Driving License")]
    DrivingLicense = 3,
}
