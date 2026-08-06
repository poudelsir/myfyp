using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities;

namespace SajhaSikshya.Data;

/// <summary>
/// Application database context. Combines ASP.NET Core Identity's user/role/claims
/// schema (via <see cref="IdentityDbContext{TUser}"/>) with SajhaSikshya's own domain
/// entities. Using <see cref="IdentityRole"/> (rather than a custom role entity) keeps
/// role management simple while the project's role set stays small and fixed
/// (see <see cref="Constants.Roles"/>).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must run first: configures the Identity schema (AspNetUsers, AspNetRoles, etc.).
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.ProfilePicturePath).HasMaxLength(300);

            // Rename Identity's default table names to a clearer, project-specific schema.
            entity.ToTable("Users");
        });

        builder.Entity<IdentityRole>(entity => entity.ToTable("Roles"));
        builder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));
        builder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));

        // Future domain entities (courses, enrollments, etc.) are registered here as
        // the project grows past the foundation phase.
    }
}
