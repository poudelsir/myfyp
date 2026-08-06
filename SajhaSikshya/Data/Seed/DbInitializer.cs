using Microsoft.AspNetCore.Identity;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;

namespace SajhaSikshya.Data.Seed;

/// <summary>
/// Seeds baseline data required for the application to function on a fresh database:
/// the fixed set of Identity roles and a default administrator account. Runs once at
/// startup (see Program.cs) after migrations have been applied.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        await SeedRolesAsync(roleManager, logger);
        await SeedDefaultAdminAsync(userManager, configuration, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (var roleName in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                logger.LogInformation("Seeded role {RoleName}", roleName);
            }
            else
            {
                logger.LogError(
                    "Failed to seed role {RoleName}: {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedDefaultAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var adminEmail = configuration["DefaultAdmin:Email"];
        var adminPassword = configuration["DefaultAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("DefaultAdmin credentials are not configured; skipping admin seed.");
            return;
        }

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to seed default admin: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        logger.LogInformation("Seeded default administrator account {Email}", adminEmail);
    }
}
