using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Users;

namespace SajhaSikshya.ViewModels.Admin.Users;

/// <summary>Backs the Admin "Manage Users" list — mirrors <c>AdminVerificationListViewModel</c>'s shape.</summary>
public class AdminUserListViewModel
{
    public PagedResult<AdminUserListItemDto> Page { get; set; } = new();

    public string? SearchTerm { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }
}
