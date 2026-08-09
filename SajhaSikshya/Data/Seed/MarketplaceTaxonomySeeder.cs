using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Utilities;

namespace SajhaSikshya.Data.Seed;

/// <summary>
/// One-time data migration that replaces the original minimal 3-category set with a
/// full Amazon/Daraz-style marketplace taxonomy (6 departments, ~65 subcategories).
/// Idempotent via a slug marker ("study-materials") — safe to leave wired into startup
/// permanently, same convention as <see cref="CatalogSeeder"/>. Unlike a fresh seed,
/// this REPARENTS the existing "Notes"/"Assignments"/"Past Papers"/"Handwritten Notes"/
/// "Video Lectures" rows (looked up by slug, never by hardcoded id) so every existing
/// <c>Listing.CategoryId</c> keeps pointing at the same row — only that row's
/// ParentCategoryId/DisplayOrder changes. Nothing is deleted. New categories link to
/// their parent via the <see cref="Category.ParentCategory"/> navigation property
/// (not a raw id) so EF's change tracker wires up the foreign key on save without
/// needing an extra round-trip per department to learn its generated id.
/// </summary>
public static class MarketplaceTaxonomySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MarketplaceTaxonomySeeder));

        if (await context.Categories.AnyAsync(c => c.Slug == "study-materials"))
        {
            return;
        }

        var studyMaterials = AddDepartment(context, "Study Materials", "layers",
            "Notes, assignments, past papers, and everything students share to study.", displayOrder: 2);
        AddSubcategories(context, studyMaterials, startOrder: 6,
            "Typed Notes", "Summary Notes", "Question Banks", "Presentations", "Lab Reports",
            "Research Papers", "Thesis / Dissertation", "Cheat Sheets", "Flash Cards", "Other Study Materials");

        var books = AddDepartment(context, "Books", "book-open",
            "Textbooks, reference books, and exam prep across every field.", displayOrder: 1);
        AddSubcategories(context, books, startOrder: 1,
            "Textbooks", "Reference Books", "Medical Books", "Nursing Books", "Engineering Books",
            "Computer Science Books", "Business & Management Books", "Law Books", "Arts & Humanities Books",
            "Entrance Preparation Books", "Competitive Exam Books", "Language Learning Books", "Other Books");

        var digitalResources = AddDepartment(context, "Digital Resources", "download",
            "PDFs, e-books, source code, and other downloadable resources.", displayOrder: 3);
        AddSubcategories(context, digitalResources, startOrder: 1,
            "PDF Books", "E-books", "Source Code", "Software", "Templates",
            "Design Files", "CAD Files", "Digital Notes", "Premium Resources", "Other Digital Files");

        var labEquipment = AddDepartment(context, "Lab & Equipment", "flask-conical",
            "Lab coats, instruments, and equipment for practical coursework.", displayOrder: 4);
        AddSubcategories(context, labEquipment, startOrder: 1,
            "Lab Coat", "Stethoscope", "Scientific Calculator", "Electronics Kit", "Medical Equipment",
            "Drawing Instruments", "Laboratory Tools", "Safety Equipment", "Other Equipment");

        var campusEssentials = AddDepartment(context, "Campus Essentials", "backpack",
            "Stationery, bags, and everyday campus life supplies.", displayOrder: 5);
        AddSubcategories(context, campusEssentials, startOrder: 1,
            "Stationery", "Bags", "Uniforms", "Hostel Essentials", "Office Supplies",
            "Accessories", "Educational Supplies", "Other Essentials");

        var donations = AddDepartment(context, "Donations", "heart-handshake",
            "Free books, notes, and equipment shared by the community.", displayOrder: 6);
        AddSubcategories(context, donations, startOrder: 1,
            "Free Books", "Free Notes", "Free Equipment", "Free Digital Resources", "Community Donations");

        await context.SaveChangesAsync();

        // Existing rows are reparented by slug lookup after the new departments exist
        // (Study Materials now has a real, saved Id), so their CategoryId FK never changes.
        var studyMaterialsId = studyMaterials.Id;
        await ReparentExistingAsync(context, logger, "notes", studyMaterialsId, displayOrder: 1);
        await ReparentExistingAsync(context, logger, "assignments", studyMaterialsId, displayOrder: 2);
        await ReparentExistingAsync(context, logger, "past-papers", studyMaterialsId, displayOrder: 3);
        await ReparentExistingAsync(context, logger, "handwritten-notes", studyMaterialsId, displayOrder: 4);
        await ReparentExistingAsync(context, logger, "video-lectures", studyMaterialsId, displayOrder: 5);

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded marketplace category taxonomy (6 departments, existing categories reparented).");
    }

    private static async Task ReparentExistingAsync(ApplicationDbContext context, ILogger logger, string slug, int newParentId, int displayOrder)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (category is null)
        {
            logger.LogWarning("MarketplaceTaxonomySeeder: expected existing category with slug '{Slug}' was not found — skipping reparent.", slug);
            return;
        }

        category.ParentCategoryId = newParentId;
        category.DisplayOrder = displayOrder;
    }

    private static Category AddDepartment(ApplicationDbContext context, string name, string iconName, string description, int displayOrder)
    {
        var department = new Category
        {
            Name = name,
            Slug = SlugHelper.Generate(name),
            IconName = iconName,
            Description = description,
            DisplayOrder = displayOrder,
            IsActive = true,
        };
        context.Categories.Add(department);
        return department;
    }

    private static void AddSubcategories(ApplicationDbContext context, Category parent, int startOrder, params string[] names)
    {
        var order = startOrder;
        foreach (var name in names)
        {
            context.Categories.Add(new Category
            {
                Name = name,
                Slug = SlugHelper.Generate(name),
                ParentCategory = parent,
                DisplayOrder = order++,
                IsActive = true,
            });
        }
    }
}
