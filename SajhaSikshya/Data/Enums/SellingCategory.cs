using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// A category of item a seller applicant intends to list, declared on their
/// <see cref="Entities.Verification.StudentVerification"/> application (multi-select).
/// For moderation and marketplace-insights purposes only — it does not constrain which
/// <see cref="Entities.Catalog.Category"/> a listing can actually use once the seller is approved.
/// Stored on the entity as a comma-separated list of these values' int form
/// (<see cref="Entities.Verification.StudentVerification.SellingCategoriesCsv"/>).
/// </summary>
public enum SellingCategory
{
    [Display(Name = "Textbooks")]
    Textbooks = 0,

    [Display(Name = "Notes & Study Materials")]
    NotesStudyMaterials = 1,

    [Display(Name = "Lab Equipment")]
    LabEquipment = 2,

    [Display(Name = "Electronics & Calculators")]
    ElectronicsCalculators = 3,

    [Display(Name = "Stationery")]
    Stationery = 4,

    [Display(Name = "Digital Resources")]
    DigitalResources = 5,

    [Display(Name = "Other Academic Materials")]
    OtherAcademicMaterials = 6,
}
