using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>Admin moderation actions on a <see cref="Entities.Reviews.Review"/> — mirrors <see cref="ModerationAction"/>'s shape for Listings: one small enum, one centralized service method, instead of three separate ad-hoc admin endpoints.</summary>
public enum ReviewModerationAction
{
    [Display(Name = "Remove", Description = "Soft-deletes an abusive or policy-violating review.")]
    Remove = 0,

    [Display(Name = "Restore", Description = "Undoes a previous removal.")]
    Restore = 1,

    [Display(Name = "Reset Report Count", Description = "Clears the report count after review — the reports were unfounded.")]
    ResetReportCount = 2,
}
