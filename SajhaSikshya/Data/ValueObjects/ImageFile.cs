using Microsoft.AspNetCore.Http;

namespace SajhaSikshya.Data.ValueObjects;

/// <summary>
/// Describes an uploaded image independently of ASP.NET Core's <see cref="IFormFile"/>,
/// so validation rules (extension, size) can be unit-tested without a real HTTP upload
/// and reused wherever an image is attached (listing photos, profile pictures, etc.).
/// </summary>
public class ImageFile
{
    public string FileName { get; }

    public string ContentType { get; }

    public long SizeInBytes { get; }

    /// <summary>Lowercased extension including the leading dot (e.g. ".jpg").</summary>
    public string Extension { get; }

    public ImageFile(string fileName, string contentType, long sizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (sizeInBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), sizeInBytes, "File size must be greater than zero.");
        }

        FileName = fileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        Extension = Path.GetExtension(fileName).ToLowerInvariant();
    }

    /// <summary>Builds an <see cref="ImageFile"/> from an uploaded <see cref="IFormFile"/>.</summary>
    public static ImageFile FromFormFile(IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new ImageFile(file.FileName, file.ContentType, file.Length);
    }

    public double SizeInMB => Math.Round(SizeInBytes / 1024.0 / 1024.0, 2);

    public bool HasAllowedExtension(IEnumerable<string> allowedExtensions) =>
        allowedExtensions.Contains(Extension, StringComparer.OrdinalIgnoreCase);

    public bool IsWithinSizeLimit(int maximumSizeMB) =>
        SizeInBytes <= maximumSizeMB * 1024L * 1024L;

    /// <summary>Convenience check combining <see cref="HasAllowedExtension"/> and <see cref="IsWithinSizeLimit"/>.</summary>
    public bool IsValid(IEnumerable<string> allowedExtensions, int maximumSizeMB) =>
        HasAllowedExtension(allowedExtensions) && IsWithinSizeLimit(maximumSizeMB);
}
