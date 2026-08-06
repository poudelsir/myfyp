using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SajhaSikshya.Validators;

/// <summary>
/// Validates that an uploaded <see cref="IFormFile"/> has one of the allowed file
/// extensions (e.g. profile picture uploads). Checking the extension is a first-pass
/// guard only — services that persist uploads must still validate content/magic bytes
/// server-side before trusting the file.
/// </summary>
public class AllowedFileExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;

    public AllowedFileExtensionsAttribute(params string[] extensions)
    {
        _extensions = extensions;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
        {
            return ValidationResult.Success;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_extensions.Contains(extension))
        {
            return new ValidationResult(
                ErrorMessage ?? $"Only the following file types are allowed: {string.Join(", ", _extensions)}");
        }

        return ValidationResult.Success;
    }
}
