using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Catalog;
using SajhaSikshya.Mappings.Catalog;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Utilities;
using SajhaSikshya.ViewModels.Admin.Catalog;

namespace SajhaSikshya.Services.Catalog;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<CategoryDto>> GetPagedAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var repository = _unitOfWork.Repository<Category>();

        var page = await repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : c => c.Name.Contains(searchTerm) || c.Slug.Contains(searchTerm),
            orderBy: q => q.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name),
            include: q => q.Include(c => c.ParentCategory).Include(c => c.Subcategories));

        return new PagedResult<CategoryDto>
        {
            Items = page.Items.Select(c => c.ToDto()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var repository = _unitOfWork.Repository<Category>();
        var category = await repository.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            return null;
        }

        if (category.ParentCategoryId.HasValue)
        {
            category.ParentCategory = await repository.GetByIdAsync(category.ParentCategoryId.Value);
        }

        return category.ToDto();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetEligibleParentCategoriesAsync(int? excludeCategoryId)
    {
        var repository = _unitOfWork.Repository<Category>();
        var allActive = await repository.FindAsync(c => c.IsActive);

        if (!excludeCategoryId.HasValue)
        {
            return allActive.OrderBy(c => c.Name).Select(c => c.ToDto()).ToList();
        }

        var excluded = CollectSelfAndDescendants(excludeCategoryId.Value, allActive);
        return allActive
            .Where(c => !excluded.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToList();
    }

    public async Task<IReadOnlySet<int>> GetCategoryAndDescendantIdsAsync(int categoryId)
    {
        var repository = _unitOfWork.Repository<Category>();
        var allCategories = await repository.GetAllAsync();
        return CollectSelfAndDescendants(categoryId, allCategories);
    }

    public async Task<IReadOnlyList<CategoryWithCountDto>> GetTopLevelCategoriesWithListingCountsAsync()
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        var allCategories = await categoryRepository.GetAllAsync();
        var departments = allCategories
            .Where(c => c.IsActive && c.ParentCategoryId is null)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToList();

        // One query for every Active listing's category, grouped in memory — avoids an
        // N+1 count query per department (there are only ~6, but this stays cheap even
        // as the taxonomy grows).
        var activeListings = await _unitOfWork.Repository<Listing>().FindAsync(l => l.Status == ListingStatus.Active);
        var countsByCategoryId = activeListings
            .GroupBy(l => l.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        return departments
            .Select(department =>
            {
                var descendantIds = CollectSelfAndDescendants(department.Id, allCategories);
                var listingCount = descendantIds.Sum(id => countsByCategoryId.GetValueOrDefault(id));

                return new CategoryWithCountDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    Slug = department.Slug,
                    IconName = department.IconName,
                    Description = department.Description,
                    ListingCount = listingCount,
                };
            })
            .ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(CategoryFormViewModel model)
    {
        var slug = SlugHelper.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug);
        if (string.IsNullOrEmpty(slug))
        {
            return ServiceResult<int>.Failure("Could not generate a valid URL slug from the provided name.");
        }

        var repository = _unitOfWork.Repository<Category>();

        if (await repository.AnyAsync(c => c.Slug == slug))
        {
            return ServiceResult<int>.Failure("A category with this slug already exists.");
        }

        if (model.ParentCategoryId.HasValue && !await repository.AnyAsync(c => c.Id == model.ParentCategoryId.Value))
        {
            return ServiceResult<int>.Failure("The selected parent category does not exist.");
        }

        var category = new Category
        {
            Name = model.Name.Trim(),
            Slug = slug,
            ParentCategoryId = model.ParentCategoryId,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            IconName = string.IsNullOrWhiteSpace(model.IconName) ? null : model.IconName.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
        };

        await repository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<int>.Success(category.Id);
    }

    public async Task<ServiceResult> UpdateAsync(CategoryFormViewModel model)
    {
        var repository = _unitOfWork.Repository<Category>();
        var category = await repository.GetByIdAsync(model.Id);

        if (category is null)
        {
            return ServiceResult.Failure("Category not found.");
        }

        if (model.ParentCategoryId == model.Id)
        {
            return ServiceResult.Failure("A category cannot be its own parent.");
        }

        if (model.ParentCategoryId.HasValue)
        {
            var allCategories = await repository.GetAllAsync();
            var descendantIds = CollectSelfAndDescendants(model.Id, allCategories);
            if (descendantIds.Contains(model.ParentCategoryId.Value))
            {
                return ServiceResult.Failure("The selected parent category is a subcategory of this category, which would create a cycle.");
            }

            if (!allCategories.Any(c => c.Id == model.ParentCategoryId.Value))
            {
                return ServiceResult.Failure("The selected parent category does not exist.");
            }
        }

        var slug = SlugHelper.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug);
        if (string.IsNullOrEmpty(slug))
        {
            return ServiceResult.Failure("Could not generate a valid URL slug from the provided name.");
        }

        if (await repository.AnyAsync(c => c.Slug == slug && c.Id != model.Id))
        {
            return ServiceResult.Failure("A category with this slug already exists.");
        }

        category.Name = model.Name.Trim();
        category.Slug = slug;
        category.ParentCategoryId = model.ParentCategoryId;
        category.DisplayOrder = model.DisplayOrder;
        category.IsActive = model.IsActive;
        category.IconName = string.IsNullOrWhiteSpace(model.IconName) ? null : model.IconName.Trim();
        category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        repository.Update(category);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var repository = _unitOfWork.Repository<Category>();
        var category = await repository.GetByIdAsync(id);

        if (category is null)
        {
            return ServiceResult.Failure("Category not found.");
        }

        if (await repository.AnyAsync(c => c.ParentCategoryId == id))
        {
            return ServiceResult.Failure("This category cannot be deleted because it has subcategories.");
        }

        if (await _unitOfWork.Repository<Listing>().AnyAsync(l => l.CategoryId == id))
        {
            return ServiceResult.Failure("This category cannot be deleted because it has listings associated with it.");
        }

        repository.Remove(category);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    /// <summary>Walks the in-memory category tree to find a category's id plus every descendant id, used to keep the parent picker acyclic.</summary>
    private static HashSet<int> CollectSelfAndDescendants(int rootId, IReadOnlyList<Category> allCategories)
    {
        var result = new HashSet<int> { rootId };
        var queue = new Queue<int>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            foreach (var child in allCategories.Where(c => c.ParentCategoryId == currentId))
            {
                if (result.Add(child.Id))
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }
}
