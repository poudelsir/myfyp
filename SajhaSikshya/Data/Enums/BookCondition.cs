using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>
/// Physical condition of a secondhand book or study material listed on the
/// marketplace, used to set buyer expectations and to filter/sort listings.
/// </summary>
public enum BookCondition
{
    /// <summary>Unused, in original condition.</summary>
    [Display(Name = "New", Description = "Unused, in original condition.")]
    New = 0,

    /// <summary>Appears unused with no visible wear.</summary>
    [Display(Name = "Like New", Description = "Appears unused with no visible wear.")]
    LikeNew = 1,

    /// <summary>Minimal signs of wear; fully intact.</summary>
    [Display(Name = "Very Good", Description = "Minimal signs of wear, fully intact.")]
    VeryGood = 2,

    /// <summary>Moderate wear but no missing pages or damage.</summary>
    [Display(Name = "Good", Description = "Moderate wear but no missing pages or damage.")]
    Good = 3,

    /// <summary>Heavily used but still fully readable and complete.</summary>
    [Display(Name = "Acceptable", Description = "Heavily used but still fully readable and complete.")]
    Acceptable = 4,

    /// <summary>Significant wear or damage; may be missing minor parts.</summary>
    [Display(Name = "Poor", Description = "Significant wear or damage; may be missing minor parts.")]
    Poor = 5,
}
