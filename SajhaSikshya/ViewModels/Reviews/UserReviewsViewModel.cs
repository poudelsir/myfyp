using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Reviews;

namespace SajhaSikshya.ViewModels.Reviews;

/// <summary>Backs the public "Seller Reviews"/"Buyer Reviews" page — reviews ABOUT one user, in one direction, with reputation stats for context.</summary>
public class UserReviewsViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public ReviewType ReviewType { get; set; }

    public int? RatingFilter { get; set; }

    public ReputationDto Reputation { get; set; } = new();

    public PagedResult<ReviewDto> Page { get; set; } = new();
}
