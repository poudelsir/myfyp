using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Reviews;

namespace SajhaSikshya.ViewModels.Reviews;

/// <summary>Backs "My Reviews" — the reviews the current user has written.</summary>
public class MyReviewsViewModel
{
    public PagedResult<ReviewDto> Page { get; set; } = new();
}
