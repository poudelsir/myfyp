using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Entities.AI;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Data.Entities.Chat;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Entities.Notifications;
using SajhaSikshya.Data.Entities.Orders;
using SajhaSikshya.Data.Entities.Reviews;
using SajhaSikshya.Data.Entities.Verification;

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

    public DbSet<University> Universities => Set<University>();

    public DbSet<AcademicLevel> AcademicLevels => Set<AcademicLevel>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

    public DbSet<SavedListing> SavedListings => Set<SavedListing>();

    public DbSet<CompareListing> CompareListings => Set<CompareListing>();

    public DbSet<StudentVerification> StudentVerifications => Set<StudentVerification>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<AIUsageLog> AIUsageLogs => Set<AIUsageLog>();

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

        // Discovers every IEntityTypeConfiguration<T> in this assembly (Data/Configurations/**)
        // so new domain entities just need a configuration class, not a line added here.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Enum storage convention: entities store enum properties (Data/Enums/**) as their
        // underlying int by default — this is EF Core's out-of-the-box behavior, so no
        // .HasConversion<string>() or global convention is needed here. Do not override this
        // per-property without a strong reason; see the Milestone "Shared Domain Enums" notes
        // for the tradeoffs. Each enum declares explicit member values (= 0, 1, 2, ...) so the
        // stored integers stay stable even if members are reordered later.
    }
}
