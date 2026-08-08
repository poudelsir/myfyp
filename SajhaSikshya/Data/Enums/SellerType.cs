using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// How a seller applicant describes themselves on their <see cref="Entities.Verification.StudentVerification"/>
/// application — self-declared, used for admin context and marketplace insights only
/// (not a separate access-control tier; every value still becomes a normal Seller once
/// approved).
/// </summary>
public enum SellerType
{
    [Display(Name = "School Student")]
    SchoolStudent = 0,

    [Display(Name = "College Student (+2)")]
    CollegeStudent = 1,

    [Display(Name = "University Student")]
    UniversityStudent = 2,

    [Display(Name = "Diploma / CTEVT Student")]
    DiplomaCtevtStudent = 3,

    [Display(Name = "Teacher / Lecturer")]
    TeacherLecturer = 4,

    [Display(Name = "Graduate / Alumni")]
    GraduateAlumni = 5,

    [Display(Name = "Educational Institution")]
    EducationalInstitution = 6,

    [Display(Name = "Independent Learner")]
    IndependentLearner = 7,

    [Display(Name = "Other")]
    Other = 8,
}
