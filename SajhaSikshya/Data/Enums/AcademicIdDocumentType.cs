using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Which academic document a seller applicant optionally uploaded to increase buyer
/// confidence (<see cref="Entities.Verification.StudentVerification.AcademicIdImagePath"/>).
/// </summary>
public enum AcademicIdDocumentType
{
    [Display(Name = "Student ID")]
    StudentId = 0,

    [Display(Name = "College ID")]
    CollegeId = 1,

    [Display(Name = "University ID")]
    UniversityId = 2,

    [Display(Name = "Teacher ID")]
    TeacherId = 3,

    [Display(Name = "Institution ID")]
    InstitutionId = 4,
}
