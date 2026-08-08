using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Extensions;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Interfaces.Verification;

namespace SajhaSikshya.Controllers;

/// <summary>
/// The one authorized path to a student ID verification document — never a public
/// static-file URL (the file lives outside <c>wwwroot</c> entirely; see
/// <see cref="IImageStorageService.SavePrivateAsync"/>). Root-level rather than
/// Area-scoped because both the Student area (viewing your own document) and the
/// Admin area (reviewing someone else's) need to reach the exact same authorization
/// check — duplicating it into two Area-scoped controllers would risk the two copies
/// drifting apart.
/// </summary>
[Authorize]
[Route("verification-images")]
public class VerificationImagesController : Controller
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly IVerificationQueryService _verificationQueryService;
    private readonly IImageStorageService _imageStorageService;

    public VerificationImagesController(IVerificationQueryService verificationQueryService, IImageStorageService imageStorageService)
    {
        _verificationQueryService = verificationQueryService;
        _imageStorageService = imageStorageService;
    }

    /// <summary>
    /// Streams one of the (up to three) documents for verification <paramref name="id"/>
    /// — <paramref name="type"/> selects which (<c>government</c>/<c>academic</c>/
    /// <c>profile</c>). Only the verification's owner or an Administrator may view it;
    /// everyone else — including another authenticated Student who isn't the owner —
    /// gets a 404, the same response as a nonexistent id or missing document, so a
    /// probing request can't distinguish "doesn't exist" from "exists but you can't see it".
    /// </summary>
    [HttpGet("{id:int}/{type}")]
    public async Task<IActionResult> Image(int id, string type)
    {
        var access = await _verificationQueryService.GetImageAccessAsync(id);
        if (access is null)
        {
            return NotFound();
        }

        var isOwner = User.GetUserId() == access.UserId;
        var isAdmin = User.IsInRole(Roles.Admin);
        if (!isOwner && !isAdmin)
        {
            return NotFound();
        }

        var imagePath = type.ToLowerInvariant() switch
        {
            "government" => access.GovernmentIdImagePath,
            "academic" => access.AcademicIdImagePath,
            "profile" => access.ProfilePhotoImagePath,
            _ => null,
        };

        if (string.IsNullOrEmpty(imagePath))
        {
            return NotFound();
        }

        var physicalPath = _imageStorageService.GetPrivatePhysicalPath(imagePath);
        if (physicalPath is null)
        {
            return NotFound();
        }

        if (!ContentTypeProvider.TryGetContentType(physicalPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(physicalPath, contentType);
    }
}
