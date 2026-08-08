namespace SajhaSikshya.DTOs.Marketplace;

/// <summary>Presentation-safe projection of <see cref="Data.Entities.Marketplace.ListingImage"/>.</summary>
public class ListingImageDto
{
    public int Id { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsThumbnail { get; set; }
}
