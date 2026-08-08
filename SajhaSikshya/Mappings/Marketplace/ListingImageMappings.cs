using SajhaSikshya.Data.Entities.Marketplace;
using SajhaSikshya.DTOs.Marketplace;

namespace SajhaSikshya.Mappings.Marketplace;

public static class ListingImageMappings
{
    public static ListingImageDto ToDto(this ListingImage image)
    {
        return new ListingImageDto
        {
            Id = image.Id,
            ImagePath = image.ImagePath,
            DisplayOrder = image.DisplayOrder,
            IsThumbnail = image.IsThumbnail,
        };
    }
}
