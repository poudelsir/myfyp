using System.ComponentModel.DataAnnotations;
using SajhaSikshya.Constants;

namespace SajhaSikshya.ViewModels.Reviews;

/// <summary>Backs both "Write a Review" and "Edit a Review" — the same shape either way, just posted to a different action.</summary>
public class WriteReviewViewModel
{
    public int OrderId { get; set; }

    /// <summary>Only set when editing an existing review.</summary>
    public int? ReviewId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public string RevieweeName { get; set; } = string.Empty;

    [Range(ReviewConstants.MinimumRating, ReviewConstants.MaximumRating, ErrorMessage = "Please choose a star rating.")]
    public int Rating { get; set; }

    [StringLength(ReviewConstants.MaximumTitleLength)]
    public string? Title { get; set; }

    [StringLength(ReviewConstants.MaximumCommentLength)]
    public string? Comment { get; set; }
}
