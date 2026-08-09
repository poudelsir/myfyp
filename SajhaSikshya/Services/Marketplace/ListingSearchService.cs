using System.Linq.Expressions;
using System.Reflection;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Catalog;
using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Marketplace;
using SajhaSikshya.Mappings.Marketplace;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces.Catalog;
using SajhaSikshya.Services.Interfaces.Marketplace;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.ViewModels.Marketplace;

namespace SajhaSikshya.Services.Marketplace;

/// <summary>
/// Implements <see cref="IListingSearchService"/>. Keyword search uses a weighted,
/// per-word relevance score rather than SQL Server full-text search — full-text search
/// requires a FULLTEXT catalog to be provisioned on the database, which is extra
/// deployment ceremony this project's LocalDB setup doesn't carry, and a plain
/// weighted-<c>Contains</c> score is portable, fully translatable by EF Core, and good
/// enough at this catalog's size. See <see cref="BuildRelevanceExpression"/> for how the
/// score is computed.
/// </summary>
public class ListingSearchService : IListingSearchService
{
    // Relative importance of a keyword match landing in each field — a Title hit is the
    // strongest signal of intent, a Description hit the weakest. Arbitrary but ordered
    // sensibly; there is no "correct" scale without real usage data to tune against.
    private const int TitleWeight = 10;
    private const int CategoryWeight = 5;
    private const int SubjectWeight = 5;
    private const int UniversityWeight = 4;
    private const int SlugWeight = 4;
    private const int SellerWeight = 3;
    private const int DescriptionWeight = 2;

    private static readonly MethodInfo StringContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    // string's "+" is compiler sugar for string.Concat, not a CLR operator overload —
    // Expression.Add (used for the numeric score arithmetic below) throws for strings,
    // so building "FirstName + \" \" + LastName" needs an explicit Concat call instead.
    private static readonly MethodInfo StringConcatMethod =
        typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })!;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryService _categoryService;
    private readonly IVerificationQueryService _verificationQueryService;

    public ListingSearchService(IUnitOfWork unitOfWork, ICategoryService categoryService, IVerificationQueryService verificationQueryService)
    {
        _unitOfWork = unitOfWork;
        _categoryService = categoryService;
        _verificationQueryService = verificationQueryService;
    }

    public async Task<PagedResult<ListingSummaryDto>> SearchAsync(ListingSearchCriteria criteria, int pageSize)
    {
        var repository = _unitOfWork.Repository<Listing>();

        var searchWords = NormalizeSearchWords(criteria.SearchTerm);
        var hasSearchTerm = searchWords.Count > 0;

        var (minPrice, maxPrice) = NormalizePriceRange(criteria.MinPrice, criteria.MaxPrice);
        var recentSinceUtc = DateTime.UtcNow.AddDays(-SearchConstants.RecentlyAddedDays);

        // Precomputed outside ApplyFilters/the expression tree — same reasoning as
        // hasSearchTerm: a HashSet<T>.Contains() capture translates to EF Core's IN
        // clause cleanly, but the async lookups themselves can't happen inside a lambda.
        var matchingCategoryIds = criteria.CategoryId.HasValue
            ? await _categoryService.GetCategoryAndDescendantIdsAsync(criteria.CategoryId.Value)
            : null;

        var verifiedSellerIds = criteria.VerifiedSellerOnly
            ? await _verificationQueryService.GetAllCurrentlyVerifiedUserIdsAsync()
            : null;

        IQueryable<Listing> ApplyFilters(IQueryable<Listing> query)
        {
            query = query.Where(l => l.Status == ListingStatus.Active);

            if (matchingCategoryIds is not null)
            {
                query = query.Where(l => matchingCategoryIds.Contains(l.CategoryId));
            }

            if (verifiedSellerIds is not null)
            {
                query = query.Where(l => verifiedSellerIds.Contains(l.SellerId));
            }

            if (criteria.UniversityId.HasValue)
            {
                query = query.Where(l => l.UniversityId == criteria.UniversityId.Value);
            }

            if (criteria.AcademicLevelId.HasValue)
            {
                query = query.Where(l => l.AcademicLevelId == criteria.AcademicLevelId.Value);
            }

            if (criteria.SubjectId.HasValue)
            {
                query = query.Where(l => l.SubjectId == criteria.SubjectId.Value);
            }

            if (criteria.Condition.HasValue)
            {
                query = query.Where(l => l.Condition == criteria.Condition.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(l => l.Price.Amount >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(l => l.Price.Amount <= maxPrice.Value);
            }

            if (criteria.DonationOnly)
            {
                query = query.Where(l => l.IsDonation);
            }

            if (criteria.RecentlyAddedOnly)
            {
                query = query.Where(l => l.CreatedAtUtc >= recentSinceUtc);
            }

            if (criteria.OnlyWithImages)
            {
                // The first image ever uploaded is always auto-assigned as the thumbnail
                // (see ListingService.UploadImagesAsync), so this alone is equivalent to
                // "has at least one image" without needing to touch the Images collection.
                query = query.Where(l => l.ThumbnailImageId != null);
            }

            // Each word ANDs onto the query as its own .Where() — a listing must contain
            // every word somewhere across the searched fields, not necessarily the same
            // field or contiguously (so "calculus notes" still matches a title like
            // "Calculus I Notes"). Chaining .Where() calls like this (rather than trying
            // to express "all words" as one Expression) is fully EF-Core-translatable
            // because each individual lambda stays simple.
            foreach (var word in searchWords)
            {
                var w = word;
                query = query.Where(l =>
                    l.Title.Contains(w)
                    || l.Description.Contains(w)
                    || l.Category.Name.Contains(w)
                    || l.Subject.Name.Contains(w)
                    || (l.University != null && l.University.Name.Contains(w))
                    || (l.Seller.FirstName + " " + l.Seller.LastName).Contains(w)
                    || l.Slug.Contains(w));
            }

            return query;
        }

        IOrderedQueryable<Listing> ApplyOrder(IQueryable<Listing> query)
        {
            // A keyword search always ranks by relevance; the chosen Sort only applies
            // when browsing without a search term (an active search term makes "most
            // relevant" the only sort order that makes sense).
            if (hasSearchTerm)
            {
                var relevance = BuildRelevanceExpression(searchWords);
                return query.OrderByDescending(relevance).ThenByDescending(l => l.CreatedAtUtc);
            }

            return criteria.Sort switch
            {
                ListingSortOption.Oldest => query.OrderBy(l => l.CreatedAtUtc),
                ListingSortOption.PriceLowToHigh => query.OrderBy(l => l.Price.Amount),
                ListingSortOption.PriceHighToLow => query.OrderByDescending(l => l.Price.Amount),
                ListingSortOption.MostViewed => query.OrderByDescending(l => l.ViewCount),
                ListingSortOption.Alphabetical => query.OrderBy(l => l.Title),
                ListingSortOption.DonationFirst => query.OrderByDescending(l => l.IsDonation).ThenByDescending(l => l.CreatedAtUtc),
                _ => query.OrderByDescending(l => l.CreatedAtUtc),
            };
        }

        var page = await repository.SearchPagedProjectedAsync(
            Math.Max(criteria.PageNumber, 1),
            pageSize,
            filter: ApplyFilters,
            orderBy: ApplyOrder,
            selector: ListingMappings.ToSummaryProjection);

        ListingMappings.ApplyDisplayFields(page.Items);
        return page;
    }

    public async Task<PagedResult<ListingSummaryDto>> GetMySavedListingsAsync(string userId, int pageNumber, int pageSize)
    {
        var savedRepository = _unitOfWork.Repository<SavedListing>();

        // Step 1: which listings (in save-order) does this page cover? A small scalar
        // projection (just the ordered ListingId) — deliberately not composing
        // ListingMappings.ToSummaryProjection across the SavedListing->Listing join here,
        // since EF Core doesn't reliably translate reusing an Expression<Func<Listing,T>>
        // rooted at a different entity type. Two small, reliable queries beat one fragile one.
        var savedPage = await savedRepository.GetPagedProjectedAsync(
            Math.Max(pageNumber, 1),
            pageSize,
            filter: s => s.UserId == userId && s.Listing.Status == ListingStatus.Active,
            orderBy: q => q.OrderByDescending(s => s.CreatedAtUtc),
            selector: s => s.ListingId);

        if (savedPage.Items.Count == 0)
        {
            return new PagedResult<ListingSummaryDto>
            {
                Items = Array.Empty<ListingSummaryDto>(),
                PageNumber = savedPage.PageNumber,
                PageSize = savedPage.PageSize,
                TotalCount = savedPage.TotalCount,
            };
        }

        // Step 2: fetch the full card projection for exactly those ids — one more query,
        // not one per row (never N+1) — then restore save-order, since a `Contains`
        // filter doesn't guarantee result order matches the id list's order.
        var orderedIds = savedPage.Items;
        var listingRepository = _unitOfWork.Repository<Listing>();
        var listings = await listingRepository.FindProjectedAsync(
            filter: l => orderedIds.Contains(l.Id),
            orderBy: q => q.OrderByDescending(l => l.CreatedAtUtc),
            take: orderedIds.Count,
            selector: ListingMappings.ToSummaryProjection);

        var orderedItems = orderedIds
            .Select(id => listings.FirstOrDefault(l => l.Id == id))
            .Where(l => l is not null)
            .Select(l => l!)
            .ToList();

        ListingMappings.ApplyDisplayFields(orderedItems);

        return new PagedResult<ListingSummaryDto>
        {
            Items = orderedItems,
            PageNumber = savedPage.PageNumber,
            PageSize = savedPage.PageSize,
            TotalCount = savedPage.TotalCount,
        };
    }

    public async Task<IReadOnlyList<ListingComparisonDto>> GetComparisonAsync(IEnumerable<int> listingIds)
    {
        var orderedIds = listingIds.ToList();
        if (orderedIds.Count == 0)
        {
            return Array.Empty<ListingComparisonDto>();
        }

        var repository = _unitOfWork.Repository<Listing>();
        var items = await repository.FindProjectedAsync(
            filter: l => l.Status == ListingStatus.Active && orderedIds.Contains(l.Id),
            orderBy: q => q.OrderByDescending(l => l.CreatedAtUtc),
            take: orderedIds.Count,
            selector: ListingMappings.ToComparisonProjection);

        ListingMappings.ApplyComparisonDisplayFields(items);

        // Restore the caller's order (guest session insertion order, or most-recently-added
        // first for the database path) — a `Contains` filter doesn't preserve it.
        return orderedIds
            .Select(id => items.FirstOrDefault(i => i.Id == id))
            .Where(i => i is not null)
            .Select(i => i!)
            .ToList();
    }

    public async Task<bool> IsListingActiveAsync(int listingId)
    {
        var repository = _unitOfWork.Repository<Listing>();
        return await repository.AnyAsync(l => l.Id == listingId && l.Status == ListingStatus.Active);
    }

    /// <summary>
    /// Trims, collapses internal whitespace, and splits the raw search term into up to
    /// <see cref="SearchConstants.MaximumSearchWords"/> words — whitespace-tolerant and
    /// bounded against pathological input (e.g. a query string with hundreds of spaces).
    /// </summary>
    private static IReadOnlyList<string> NormalizeSearchWords(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<string>();
        }

        var trimmed = searchTerm.Trim();
        if (trimmed.Length > SearchConstants.MaximumSearchTermLength)
        {
            trimmed = trimmed[..SearchConstants.MaximumSearchTermLength];
        }

        return trimmed
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Take(SearchConstants.MaximumSearchWords)
            .ToList();
    }

    /// <summary>Clamps both bounds into <see cref="ListingConstants"/>'s valid price range and swaps them if given in reverse order, so a caller-supplied <c>minPrice &gt; maxPrice</c> never produces an always-empty query.</summary>
    private static (decimal? Min, decimal? Max) NormalizePriceRange(decimal? minPrice, decimal? maxPrice)
    {
        var min = minPrice.HasValue ? Math.Clamp(minPrice.Value, ListingConstants.MinimumPrice, ListingConstants.MaximumPrice) : (decimal?)null;
        var max = maxPrice.HasValue ? Math.Clamp(maxPrice.Value, ListingConstants.MinimumPrice, ListingConstants.MaximumPrice) : (decimal?)null;

        if (min.HasValue && max.HasValue && min.Value > max.Value)
        {
            (min, max) = (max, min);
        }

        return (min, max);
    }

    /// <summary>
    /// Builds <c>l =&gt; score</c> where score sums a per-word, per-field weighted hit
    /// count (see the Weight constants above) via dynamic <see cref="Expression"/>
    /// construction — the number of words isn't known until runtime, so this can't be
    /// written as a fixed lambda the way the rest of the query is. Every sub-expression
    /// (property access, <see cref="string.Contains(string)"/>, ternary, addition) is a
    /// primitive EF Core already translates elsewhere in this file, so the composed
    /// expression is translatable too.
    /// </summary>
    private static Expression<Func<Listing, int>> BuildRelevanceExpression(IReadOnlyList<string> words)
    {
        var listingParameter = Expression.Parameter(typeof(Listing), "l");

        Expression? totalScore = null;

        foreach (var word in words)
        {
            var wordConstant = Expression.Constant(word);

            var titleScore = FieldScore(Expression.Property(listingParameter, nameof(Listing.Title)), wordConstant, TitleWeight);
            var descriptionScore = FieldScore(Expression.Property(listingParameter, nameof(Listing.Description)), wordConstant, DescriptionWeight);
            var slugScore = FieldScore(Expression.Property(listingParameter, nameof(Listing.Slug)), wordConstant, SlugWeight);

            var categoryName = Expression.Property(Expression.Property(listingParameter, nameof(Listing.Category)), nameof(Category.Name));
            var categoryScore = FieldScore(categoryName, wordConstant, CategoryWeight);

            var subjectName = Expression.Property(Expression.Property(listingParameter, nameof(Listing.Subject)), nameof(Subject.Name));
            var subjectScore = FieldScore(subjectName, wordConstant, SubjectWeight);

            var universityProperty = Expression.Property(listingParameter, nameof(Listing.University));
            var universityNotNull = Expression.NotEqual(universityProperty, Expression.Constant(null, typeof(University)));
            var universityNameContains = Expression.Call(Expression.Property(universityProperty, nameof(University.Name)), StringContainsMethod, wordConstant);
            var universityScore = Expression.Condition(
                Expression.AndAlso(universityNotNull, universityNameContains),
                Expression.Constant(UniversityWeight),
                Expression.Constant(0));

            var sellerFullName = Expression.Call(
                StringConcatMethod,
                Expression.Call(
                    StringConcatMethod,
                    Expression.Property(Expression.Property(listingParameter, nameof(Listing.Seller)), "FirstName"),
                    Expression.Constant(" ")),
                Expression.Property(Expression.Property(listingParameter, nameof(Listing.Seller)), "LastName"));
            var sellerScore = FieldScore(sellerFullName, wordConstant, SellerWeight);

            var wordTotal = new[] { titleScore, descriptionScore, categoryScore, subjectScore, universityScore, sellerScore, slugScore }
                .Aggregate(Expression.Add);

            totalScore = totalScore is null ? wordTotal : Expression.Add(totalScore, wordTotal);
        }

        return Expression.Lambda<Func<Listing, int>>(totalScore!, listingParameter);
    }

    /// <summary>Builds <c>fieldAccess.Contains(word) ? weight : 0</c> for a single field.</summary>
    private static Expression FieldScore(Expression fieldAccess, ConstantExpression word, int weight)
    {
        var contains = Expression.Call(fieldAccess, StringContainsMethod, word);
        return Expression.Condition(contains, Expression.Constant(weight), Expression.Constant(0));
    }
}
