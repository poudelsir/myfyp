using Microsoft.AspNetCore.Http;

namespace SajhaSikshya.Services.Interfaces;

/// <summary>
/// Generic, domain-agnostic file storage. Deliberately knows nothing about Listings —
/// the caller supplies a subfolder and its own size/extension/MIME-type limits — so the
/// same service backs listing photos, verification documents, and (as of Phase 7.3)
/// chat attachments (images and documents alike) without change. Two storage roots are
/// supported:
/// the public <c>SaveAsync</c>/<c>DeleteAsync</c> pair (under <c>wwwroot/uploads/</c>,
/// reachable by anyone via a public URL through static-file middleware — correct for
/// listing photos, which are meant to be public) and the private
/// <c>SavePrivateAsync</c>/<c>GetPrivatePhysicalPath</c>/<c>DeletePrivateAsync</c> set
/// (outside <c>wwwroot</c> entirely, structurally unreachable by static-file middleware
/// regardless of configuration — for documents like student ID verification uploads
/// that must only ever be served through an authorized controller action).
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Validates <paramref name="file"/> (extension, MIME type, size, and file-content
    /// signature — never just the client-supplied name/type) and saves it under
    /// <c>wwwroot/uploads/{subfolder}/</c> with a newly generated, unique filename.
    /// Returns the web-relative path (e.g. "/uploads/listings/42/&lt;guid&gt;.jpg") on success.
    /// <paramref name="allowedMimeTypes"/> defaults to the original image-only allow-list
    /// (jpeg/png/webp) when omitted, so every existing caller (Listing photos,
    /// verification documents) keeps its exact original behavior unchanged; Chat
    /// attachments (Phase 7.3) are the first caller to pass a broader list (documents),
    /// which is why this parameter exists rather than the allow-list staying hardcoded.
    /// </summary>
    Task<ServiceResult<string>> SaveAsync(
        IFormFile file,
        string subfolder,
        IReadOnlyList<string> allowedExtensions,
        int maxSizeMB,
        IReadOnlyList<string>? allowedMimeTypes = null);

    /// <summary>
    /// Deletes a previously-saved public file by its web-relative path. Safe to call even
    /// if the file is already gone (e.g. a retry after a partial failure) — this never
    /// throws for a missing file, only logs and continues, since callers use it for
    /// best-effort cleanup.
    /// </summary>
    Task DeleteAsync(string relativePath);

    /// <summary>
    /// Same validation as <see cref="SaveAsync"/>, but saves outside <c>wwwroot</c> — never
    /// reachable by a public URL under any static-file configuration. Returns an opaque
    /// storage key (e.g. "verifications/&lt;guid&gt;.jpg"), not a URL; callers must go
    /// through <see cref="GetPrivatePhysicalPath"/> plus their own authorization check to
    /// ever read the file back.
    /// </summary>
    Task<ServiceResult<string>> SavePrivateAsync(
        IFormFile file,
        string subfolder,
        IReadOnlyList<string> allowedExtensions,
        int maxSizeMB,
        IReadOnlyList<string>? allowedMimeTypes = null);

    /// <summary>
    /// Resolves a key previously returned by <see cref="SavePrivateAsync"/> to a physical
    /// file path for authorized streaming (e.g. via <c>PhysicalFileResult</c>) — the
    /// caller is responsible for checking the requester is allowed to see it before
    /// calling this. Returns null if the key doesn't resolve to an existing file within
    /// the private storage root (refuses anything that would resolve outside it, the same
    /// path-traversal guard <see cref="DeleteAsync"/> already uses for the public root).
    /// </summary>
    string? GetPrivatePhysicalPath(string relativePath);

    /// <summary>Deletes a previously-saved private file by its storage key. Same best-effort semantics as <see cref="DeleteAsync"/>.</summary>
    Task DeletePrivateAsync(string relativePath);
}
