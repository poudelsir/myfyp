using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Authorization;
using SajhaSikshya.Configurations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Repositories;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services;
using SajhaSikshya.Services.AI;
using SajhaSikshya.Services.Catalog;
using SajhaSikshya.Services.Chat;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Dashboard;
using SajhaSikshya.Services.Interfaces.AI;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Chat;
using SajhaSikshya.Services.Interfaces.Dashboard;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Orders;
using SajhaSikshya.Services.Interfaces.Reviews;
using SajhaSikshya.Services.Interfaces.Users;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.Services.Marketplace;
using SajhaSikshya.Services.Notifications;
using SajhaSikshya.Services.Orders;
using SajhaSikshya.Services.Reviews;
using SajhaSikshya.Services.Users;
using SajhaSikshya.Services.Verification;

namespace SajhaSikshya.Extensions;

/// <summary>
/// Groups all dependency-injection registration into focused extension methods so
/// Program.cs stays a short, readable list of intent ("add persistence", "add
/// identity", "add application services") instead of a wall of AddScoped calls.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers EF Core with SQL Server using the "DefaultConnection" connection string.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        return services;
    }

    /// <summary>
    /// Configures ASP.NET Core Identity: password/lockout policy, the EF Core stores,
    /// and cookie behavior for authenticated sessions.
    /// </summary>
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(SecurityConstants.LockoutMinutes);
                options.Lockout.MaxFailedAccessAttempts = SecurityConstants.MaxLoginAttempts;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        // Identity's default token provider (registered above) backs password reset
        // tokens too — shorten its lifespan from the 1-day default for that
        // higher-stakes flow, rather than hand-rolling a separate token mechanism.
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(SecurityConstants.PasswordResetTokenLifespanHours);
        });

        // Makes Suspend User actually take effect on an already-signed-in session — see
        // SecurityConstants.SecurityStampValidationIntervalMinutes' remarks.
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromMinutes(SecurityConstants.SecurityStampValidationIntervalMinutes);
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        return services;
    }

    /// <summary>Registers the generic repository and unit of work abstractions.</summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>Registers application services (business logic layer).</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IImageStorageService, ImageStorageService>();

        services.AddScoped<IUniversityService, UniversityService>();
        services.AddScoped<IAcademicLevelService, AcademicLevelService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IListingQueryService, ListingQueryService>();
        services.AddScoped<IListingSearchService, ListingSearchService>();
        services.AddScoped<ISavedListingService, SavedListingService>();
        services.AddScoped<ICompareService, CompareService>();
        services.AddScoped<IVerificationService, VerificationService>();
        services.AddScoped<IVerificationQueryService, VerificationQueryService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IChatQueryService, ChatQueryService>();
        services.AddScoped<IChatNotificationDispatcher, SignalRChatNotificationDispatcher>();

        // Singleton: presence must be shared across every Hub instance (Hubs are
        // transient, one per invocation) and outlive any single connection.
        services.AddSingleton<IChatPresenceTracker, InMemoryChatPresenceTracker>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();
        services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();

        // Typed HttpClient — one pooled, reused HttpMessageHandler per IHttpClientFactory
        // conventions, rather than a `new HttpClient()` inside GeminiAIService itself.
        services.AddHttpClient<IAIService, GeminiAIService>();
        services.AddScoped<IListingAIService, ListingAIService>();
        services.AddScoped<IMarketplaceAssistantService, MarketplaceAssistantService>();
        services.AddScoped<IAdminInsightsService, AdminInsightsService>();

        services.AddScoped<IDashboardQueryService, DashboardQueryService>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="AuthorizationPolicies.VerifiedStudent"/> policy and its
    /// handler (Phase 5). Registered and available for use, but not applied to any
    /// endpoint yet — a future phase adds <c>[Authorize(Policy = AuthorizationPolicies.VerifiedStudent)]</c>
    /// to Create Listing, Purchase, Donate, and Chat once those exist.
    /// </summary>
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, VerifiedStudentHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.VerifiedStudent, policy =>
                policy.Requirements.Add(new VerifiedStudentRequirement()));
        });

        return services;
    }

    /// <summary>Binds strongly-typed configuration sections used by the Options pattern.</summary>
    public static IServiceCollection AddApplicationConfigurations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<ApplicationSettings>(configuration.GetSection(ApplicationSettings.SectionName));
        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));
        return services;
    }
}
