using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Configurations;
using SajhaSikshya.Data;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Repositories;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services;
using SajhaSikshya.Services.Interfaces;

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

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

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
        return services;
    }

    /// <summary>Binds strongly-typed configuration sections used by the Options pattern.</summary>
    public static IServiceCollection AddApplicationConfigurations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<ApplicationSettings>(configuration.GetSection(ApplicationSettings.SectionName));
        return services;
    }
}
