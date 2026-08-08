using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Catalog;

namespace SajhaSikshya.Data.Seed;

/// <summary>
/// Seeds a small set of sample catalog data (universities, academic levels, subjects,
/// categories) so the Admin CRUD screens have real data to exercise on a fresh
/// database. Each block checks for existing rows first so re-running on an already
/// seeded database is a no-op.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(CatalogSeeder));

        var universities = await SeedUniversitiesAsync(context, logger);
        var levels = await SeedAcademicLevelsAsync(context, logger);
        await SeedCategoriesAsync(context, logger);
        await SeedSubjectsAsync(context, levels, universities, logger);
    }

    private static async Task<List<University>> SeedUniversitiesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Universities.AnyAsync())
        {
            return await context.Universities.ToListAsync();
        }

        var universities = new List<University>
        {
            new() { Name = "Tribhuvan University", Code = "TU", City = "Kirtipur", IsActive = true },
            new() { Name = "Kathmandu University", Code = "KU", City = "Dhulikhel", IsActive = true },
            new() { Name = "Pokhara University", Code = "PU", City = "Pokhara", IsActive = true },
        };

        context.Universities.AddRange(universities);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} universities", universities.Count);

        return universities;
    }

    private static async Task<List<AcademicLevel>> SeedAcademicLevelsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.AcademicLevels.AnyAsync())
        {
            return await context.AcademicLevels.ToListAsync();
        }

        var levels = new List<AcademicLevel>
        {
            new() { Name = "+2 / High School", Code = "PLUS2", DisplayOrder = 1, IsActive = true },
            new() { Name = "Bachelor's Degree", Code = "BACHELOR", DisplayOrder = 2, IsActive = true },
            new() { Name = "Master's Degree", Code = "MASTER", DisplayOrder = 3, IsActive = true },
        };

        context.AcademicLevels.AddRange(levels);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} academic levels", levels.Count);

        return levels;
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var notes = new Category { Name = "Notes", Slug = "notes", DisplayOrder = 1, IsActive = true };
        var assignments = new Category { Name = "Assignments", Slug = "assignments", DisplayOrder = 2, IsActive = true };
        var pastPapers = new Category { Name = "Past Papers", Slug = "past-papers", DisplayOrder = 3, IsActive = true };
        var handwrittenNotes = new Category
        {
            Name = "Handwritten Notes",
            Slug = "handwritten-notes",
            DisplayOrder = 1,
            IsActive = true,
            ParentCategory = notes,
        };

        context.Categories.AddRange(notes, assignments, pastPapers, handwrittenNotes);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded 4 categories");
    }

    private static async Task SeedSubjectsAsync(
        ApplicationDbContext context,
        List<AcademicLevel> levels,
        List<University> universities,
        ILogger logger)
    {
        if (await context.Subjects.AnyAsync())
        {
            return;
        }

        var bachelor = levels.First(l => l.Code == "BACHELOR");
        var tu = universities.First(u => u.Code == "TU");
        var ku = universities.First(u => u.Code == "KU");

        var subjects = new List<Subject>
        {
            new() { Name = "Data Structures and Algorithms", Code = "CSC202", AcademicLevel = bachelor, University = tu, IsActive = true },
            new() { Name = "Database Management Systems", Code = "CSC205", AcademicLevel = bachelor, University = tu, IsActive = true },
            new() { Name = "Computer Networks", Code = "CSC301", AcademicLevel = bachelor, University = ku, IsActive = true },
            new() { Name = "Calculus I", Code = "MTH101", AcademicLevel = bachelor, University = null, IsActive = true },
        };

        context.Subjects.AddRange(subjects);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} subjects", subjects.Count);
    }
}
