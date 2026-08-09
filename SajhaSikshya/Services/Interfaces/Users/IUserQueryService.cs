using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Users;

namespace SajhaSikshya.Services.Interfaces.Users;

/// <summary>
/// Read-only Admin user queries. <see cref="Data.Entities.ApplicationUser"/> doesn't
/// inherit <see cref="Data.Entities.BaseEntity"/> so it can't go through the generic
/// <c>IUnitOfWork.Repository&lt;T&gt;()</c> pattern the rest of the app uses — this
/// queries <c>UserManager&lt;ApplicationUser&gt;.Users</c> directly instead, the same
/// precedent <c>DashboardQueryService</c> already established for admin user aggregates.
/// </summary>
public interface IUserQueryService
{
    /// <summary>Search by name/email, optionally filtered by role and account-active status.</summary>
    Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(string? searchTerm, string? role, bool? isActive, int pageNumber, int pageSize);

    /// <summary>Null if no user with this id exists.</summary>
    Task<AdminUserDetailDto?> GetUserDetailAsync(string userId);
}
