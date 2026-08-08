using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SajhaSikshya.Data.ValueObjects;
using SajhaSikshya.Services.Interfaces;

namespace SajhaSikshya.Services;

/// <summary>
/// Local-disk implementation of <see cref="IImageStorageService"/>. The public root
/// (<c>wwwroot/uploads/</c>) backs listing photos and anything else meant to be
/// reachable by a plain URL; the private root (a sibling folder outside
/// <c>wwwroot</c> entirely) backs documents like student ID verification uploads that
/// must only ever be served through an authorized controller action. Every upload,
/// public or private, passes the same four independent checks before a single byte is
/// written: allowed extension, allowed MIME type, size limit, and a file-content
/// signature check (magic bytes) — the first three are all attacker-controlled
/// (filename, Content-Type header, declared length), so the signature check is what
/// actually stops a renamed executable from being accepted as a "photo".
/// </summary>
public class ImageStorageService : IImageStorageService
{
    private const string UploadsRootFolder = "uploads";
    private const string PrivateRootFolder = "PrivateUploads";

    /// <summary>Used whenever a caller omits <c>allowedMimeTypes</c> — the original, unchanged behavior for every caller that predates Phase 7.3 (Listing photos, verification documents).</summary>
    private static readonly IReadOnlyList<string> DefaultImageMimeTypes = new[]
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
    private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 }; // "PK\3\4" — DOCX (and any Office Open XML format) is a ZIP container; the format itself has no more specific magic number.

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(IWebHostEnvironment environment, ILogger<ImageStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    private string PublicRoot => Path.Combine(_environment.WebRootPath, UploadsRootFolder);

    // ContentRootPath is the application's own root directory (the parent of wwwroot in
    // the standard project layout) — static-file middleware only ever serves out of
    // WebRootPath, so anything under ContentRootPath but outside WebRootPath is
    // structurally unreachable by a URL, not just hidden by configuration.
    private string PrivateRoot => Path.Combine(_environment.ContentRootPath, PrivateRootFolder);

    public async Task<ServiceResult<string>> SaveAsync(
        IFormFile file,
        string subfolder,
        IReadOnlyList<string> allowedExtensions,
        int maxSizeMB,
        IReadOnlyList<string>? allowedMimeTypes = null)
    {
        var result = await SaveToRootAsync(file, subfolder, allowedExtensions, maxSizeMB, PublicRoot, allowedMimeTypes);
        if (!result.Succeeded)
        {
            return result;
        }

        // The public root's storage key becomes a real, servable URL — the private
        // root's does not (see SavePrivateAsync), which is the one difference between
        // the two beyond which physical folder they write into.
        return ServiceResult<string>.Success($"/{UploadsRootFolder}/{result.Data}");
    }

    public Task<ServiceResult<string>> SavePrivateAsync(
        IFormFile file,
        string subfolder,
        IReadOnlyList<string> allowedExtensions,
        int maxSizeMB,
        IReadOnlyList<string>? allowedMimeTypes = null) =>
        SaveToRootAsync(file, subfolder, allowedExtensions, maxSizeMB, PrivateRoot, allowedMimeTypes);

    public Task DeleteAsync(string relativePath)
    {
        var physicalPath = ResolvePhysicalPath(PublicRoot, StripPublicUrlPrefix(relativePath));
        if (physicalPath is not null)
        {
            TryDeleteFile(physicalPath);
        }

        return Task.CompletedTask;
    }

    public Task DeletePrivateAsync(string relativePath)
    {
        var physicalPath = ResolvePhysicalPath(PrivateRoot, relativePath);
        if (physicalPath is not null)
        {
            TryDeleteFile(physicalPath);
        }

        return Task.CompletedTask;
    }

    public string? GetPrivatePhysicalPath(string relativePath)
    {
        var physicalPath = ResolvePhysicalPath(PrivateRoot, relativePath);
        return physicalPath is not null && File.Exists(physicalPath) ? physicalPath : null;
    }

    /// <summary>Shared save mechanics for both storage roots — validation, magic-byte check, and a GUID filename are identical either way; only the destination root differs. Returns a bare "subfolder/file.ext" key with no root prefix — <see cref="SaveAsync"/> adds the public URL prefix itself.</summary>
    private async Task<ServiceResult<string>> SaveToRootAsync(
        IFormFile file,
        string subfolder,
        IReadOnlyList<string> allowedExtensions,
        int maxSizeMB,
        string root,
        IReadOnlyList<string>? allowedMimeTypes)
    {
        if (file.Length == 0)
        {
            return ServiceResult<string>.Failure($"'{file.FileName}' is empty.");
        }

        // ImageFile's name predates this service supporting non-image documents, but its
        // extension/size checks were always format-agnostic — reused as-is rather than
        // renamed, to keep this an additive extension, not a rename sweep across every
        // existing caller.
        var imageFile = ImageFile.FromFormFile(file);
        var effectiveMimeTypes = allowedMimeTypes ?? DefaultImageMimeTypes;

        if (!imageFile.HasAllowedExtension(allowedExtensions))
        {
            return ServiceResult<string>.Failure(
                $"'{imageFile.FileName}' is not an allowed file type. Allowed types: {string.Join(", ", allowedExtensions)}.");
        }

        if (!effectiveMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return ServiceResult<string>.Failure($"'{imageFile.FileName}' has an unrecognized file type.");
        }

        if (!imageFile.IsWithinSizeLimit(maxSizeMB))
        {
            return ServiceResult<string>.Failure($"'{imageFile.FileName}' exceeds the maximum size of {maxSizeMB} MB.");
        }

        await using var uploadStream = file.OpenReadStream();

        if (!await HasValidFileSignatureAsync(uploadStream, imageFile.Extension))
        {
            return ServiceResult<string>.Failure($"'{imageFile.FileName}' does not appear to be a valid file of its claimed type.");
        }

        uploadStream.Position = 0;

        // The client's filename is used only to read the extension above — it never
        // touches the filesystem. The stored name is always a fresh GUID.
        var safeSubfolder = SanitizeSubfolder(subfolder);
        var targetDirectory = Path.Combine(root, safeSubfolder);
        Directory.CreateDirectory(targetDirectory);

        var secureFileName = $"{Guid.NewGuid():N}{imageFile.Extension}";
        var physicalPath = Path.Combine(targetDirectory, secureFileName);

        try
        {
            // FileMode.CreateNew refuses to silently overwrite if the (astronomically
            // unlikely) GUID collision ever happened, instead of masking it.
            await using var destination = new FileStream(
                physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await uploadStream.CopyToAsync(destination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save uploaded image to {Path}", physicalPath);
            TryDeleteFile(physicalPath);
            return ServiceResult<string>.Failure($"Failed to save '{imageFile.FileName}'. Please try again.");
        }

        return ServiceResult<string>.Success($"{safeSubfolder}/{secureFileName}");
    }

    /// <summary>SaveAsync's stored value includes the "/uploads/" URL prefix (its established, unchanged public contract); the delete path needs the bare key relative to <see cref="PublicRoot"/> instead.</summary>
    private static string StripPublicUrlPrefix(string relativePath) =>
        relativePath.TrimStart('/', '\\').StartsWith(UploadsRootFolder, StringComparison.OrdinalIgnoreCase)
            ? relativePath.TrimStart('/', '\\')[UploadsRootFolder.Length..].TrimStart('/', '\\')
            : relativePath;

    /// <summary>
    /// Resolves a stored relative path/key back to a physical path under <paramref name="root"/>,
    /// refusing to return one outside it — the defense against path traversal (e.g. a
    /// stored value somehow containing "../../"). Nothing in this app currently
    /// constructs relative paths from raw user input, but this makes the guarantee
    /// structural rather than dependent on every caller remembering to sanitize.
    /// </summary>
    private string? ResolvePhysicalPath(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var rootFull = Path.GetFullPath(root);
        var combined = Path.GetFullPath(Path.Combine(root, relativePath.TrimStart('/', '\\')));

        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Refused to resolve a path outside its storage root: {Path}", relativePath);
            return null;
        }

        return combined;
    }

    private void TryDeleteFile(string physicalPath)
    {
        try
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete file {Path} (likely locked); it will remain an orphan until manually cleaned up.", physicalPath);
        }
    }

    /// <summary>Subfolders are always built by our own code (e.g. "listings/42"), never taken verbatim from user input — this is a defense-in-depth normalization pass, not the primary safeguard.</summary>
    private static string SanitizeSubfolder(string subfolder)
    {
        var segments = subfolder.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
        var safeSegments = segments.Select(segment =>
            string.Concat(segment.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')));
        return string.Join('/', safeSegments);
    }

    /// <summary>
    /// Reads the first bytes of the stream and checks them against the known magic-byte
    /// signature for the claimed extension. This is the check that actually matters: a
    /// file renamed from ".exe" to ".jpg" fails here even though it passes the extension,
    /// MIME-type, and size checks (all three of which the client fully controls).
    ///
    /// ".docx" only checks for the ZIP container signature — Office Open XML's format
    /// has no more specific magic number than "this is a ZIP file" (a DOCX is a ZIP of
    /// XML parts), so a renamed generic .zip would pass this specific check; the
    /// extension/MIME-type checks upstream are what narrow it to DOCX specifically for
    /// that case. ".txt" has no formal magic number at all (arbitrary bytes are valid
    /// plain text) — this instead rejects anything that looks unambiguously *binary*
    /// (a NUL byte, which never legitimately appears in text), the standard practical
    /// heuristic for "is this plausibly a text file" absent a real signature.
    /// </summary>
    private static async Task<bool> HasValidFileSignatureAsync(Stream stream, string extension)
    {
        var buffer = new byte[12];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

        return extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".png" => bytesRead >= 8 && buffer.AsSpan(0, 8).SequenceEqual(PngSignature),
            ".webp" => bytesRead >= 12
                && buffer.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && buffer.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            ".pdf" => bytesRead >= 4 && buffer.AsSpan(0, 4).SequenceEqual(PdfSignature),
            ".docx" => bytesRead >= 4 && buffer.AsSpan(0, 4).SequenceEqual(ZipSignature),
            ".txt" => bytesRead == 0 || !buffer.AsSpan(0, bytesRead).Contains((byte)0),
            _ => false,
        };
    }
}
