using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Constants;
using SajhaSikshya.Data.Entities;
using SajhaSikshya.Data.Entities.Verification;
using SajhaSikshya.Data.Enums;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.Interfaces;
using SajhaSikshya.Services.Interfaces.Notifications;
using SajhaSikshya.Services.Interfaces.Verification;
using SajhaSikshya.Services.Notifications;
using SajhaSikshya.ViewModels.Verification;

namespace SajhaSikshya.Services.Verification;

/// <summary>
/// Implements <see cref="IVerificationService"/>. History is never overwritten: a
/// resubmission always inserts a brand new <see cref="StudentVerification"/> row
/// rather than resetting an existing Rejected one back to Pending — this is what makes
/// "keep every past attempt" (Phase 5's explicit requirement) automatic rather than
/// something every write path has to remember to preserve, and it's also why
/// <see cref="CreateAsync"/> and <see cref="ResubmitAsync"/> can safely share the exact
/// same insert logic — from the database's point of view a resubmission isn't
/// materially different from a first submission, just a user who already has rows in
/// their history.
///
/// Both a submission and an admin's review notify the student (Phase 8.2) via
/// <see cref="_notificationService"/> — a submission is a self-confirmation receipt,
/// a review's exact wording depends on which <see cref="VerificationAction"/> the
/// reviewer took. A submission also broadcasts to every Admin (the same
/// <see cref="INotificationService.CreateBroadcastAsync"/> pattern
/// <c>ReviewService.ReportAsync</c> uses for a flagged review) so the review queue
/// doesn't rely on an admin noticing it on their own.
/// </summary>
public class VerificationService : IVerificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _imageStorageService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IUnitOfWork unitOfWork,
        IImageStorageService imageStorageService,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        ILogger<VerificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _imageStorageService = imageStorageService;
        _notificationService = notificationService;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
    }

    public Task<ServiceResult<int>> CreateAsync(string userId, VerificationSubmissionViewModel model) =>
        SubmitAsync(userId, model);

    public Task<ServiceResult<int>> ResubmitAsync(string userId, VerificationSubmissionViewModel model) =>
        SubmitAsync(userId, model);

    private async Task<ServiceResult<int>> SubmitAsync(string userId, VerificationSubmissionViewModel model)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();

        var hasPending = await repository.AnyAsync(v => v.UserId == userId && v.Status == VerificationStatus.Pending);
        if (hasPending)
        {
            return ServiceResult<int>.Failure("You already have a verification request pending review.");
        }

        if (model.GovernmentIdImage is null)
        {
            return ServiceResult<int>.Failure("Please upload a government-issued identity document.");
        }

        if (model.ProfilePhoto is null)
        {
            return ServiceResult<int>.Failure("Please upload a recent photo of yourself.");
        }

        // SavePrivateAsync (not SaveAsync) — verification documents are stored outside
        // wwwroot entirely, never reachable by a public URL. See IImageStorageService's
        // remarks and VerificationImagesController for the authorized read path. Every
        // path saved this call is tracked so a later failure can roll all of them back,
        // not just the first one.
        var savedPaths = new List<string>();

        var governmentIdResult = await _imageStorageService.SavePrivateAsync(
            model.GovernmentIdImage,
            VerificationConstants.ImageStorageSubfolder,
            VerificationConstants.AllowedDocumentExtensions,
            VerificationConstants.MaximumDocumentSizeMB,
            VerificationConstants.AllowedDocumentMimeTypes);

        if (!governmentIdResult.Succeeded)
        {
            return ServiceResult<int>.Failure(governmentIdResult.Errors.FirstOrDefault() ?? "The government ID document could not be uploaded.");
        }

        savedPaths.Add(governmentIdResult.Data!);

        var profilePhotoResult = await _imageStorageService.SavePrivateAsync(
            model.ProfilePhoto,
            VerificationConstants.ImageStorageSubfolder,
            VerificationConstants.AllowedImageExtensions,
            VerificationConstants.MaximumImageSizeMB);

        if (!profilePhotoResult.Succeeded)
        {
            await RollbackSavedFilesAsync(savedPaths);
            return ServiceResult<int>.Failure(profilePhotoResult.Errors.FirstOrDefault() ?? "The profile photo could not be uploaded.");
        }

        savedPaths.Add(profilePhotoResult.Data!);

        string? academicIdPath = null;
        if (model.AcademicIdImage is not null)
        {
            var academicIdResult = await _imageStorageService.SavePrivateAsync(
                model.AcademicIdImage,
                VerificationConstants.ImageStorageSubfolder,
                VerificationConstants.AllowedDocumentExtensions,
                VerificationConstants.MaximumDocumentSizeMB,
                VerificationConstants.AllowedDocumentMimeTypes);

            if (!academicIdResult.Succeeded)
            {
                await RollbackSavedFilesAsync(savedPaths);
                return ServiceResult<int>.Failure(academicIdResult.Errors.FirstOrDefault() ?? "The academic ID document could not be uploaded.");
            }

            academicIdPath = academicIdResult.Data!;
            savedPaths.Add(academicIdPath);
        }

        var verification = new StudentVerification
        {
            UserId = userId,
            GovernmentIdImagePath = governmentIdResult.Data!,
            GovernmentIdDocumentType = model.GovernmentIdDocumentType,
            AcademicIdImagePath = academicIdPath,
            AcademicIdDocumentType = model.AcademicIdDocumentType,
            ProfilePhotoImagePath = profilePhotoResult.Data!,
            SellerType = model.SellerType,
            InstitutionName = string.IsNullOrWhiteSpace(model.InstitutionName) ? null : model.InstitutionName.Trim(),
            SellingCategoriesCsv = string.Join(",", model.SellingCategories.Select(c => (int)c)),
            DeclarationAccepted = model.DeclarationAccepted,
            Status = VerificationStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
        };

        try
        {
            await repository.AddAsync(verification);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // No database row was written — every uploaded file this call is now
            // orphaned, so all of them are removed rather than left behind. Mirrors
            // ListingService.UploadImagesAsync's save-then-rollback-on-failure shape.
            _logger.LogError(ex, "Failed to save verification submission for user {UserId}", userId);
            await RollbackSavedFilesAsync(savedPaths);
            return ServiceResult<int>.Failure("Failed to submit your verification. Please try again.");
        }

        var (submittedTitle, submittedMessage) = NotificationTemplates.VerificationSubmitted();
        await _notificationService.CreateAsync(userId, NotificationType.Verification, submittedTitle, submittedMessage, VerificationLink, createdBy: userId);

        var student = await _userManager.FindByIdAsync(userId);
        var (adminTitle, adminMessage) = NotificationTemplates.NewVerificationRequest(student?.FullName ?? "A student");
        await _notificationService.CreateBroadcastAsync(NotificationType.Verification, adminTitle, adminMessage, AdminVerificationQueueLink, userId, targetRole: Roles.Admin);

        return ServiceResult<int>.Success(verification.Id);
    }

    private async Task RollbackSavedFilesAsync(IReadOnlyList<string> savedPaths)
    {
        foreach (var path in savedPaths)
        {
            await _imageStorageService.DeletePrivateAsync(path);
        }
    }

    public async Task<ServiceResult> ReviewAsync(int verificationId, VerificationAction action, string reviewerId, string? reason = null, string? adminNotes = null)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();
        var verification = await repository.GetByIdAsync(verificationId);
        if (verification is null)
        {
            return ServiceResult.Failure("Verification request not found.");
        }

        if (!VerificationState.CanReview(verification.Status))
        {
            return ServiceResult.Failure("Only requests pending review can be reviewed.");
        }

        if (VerificationState.RequiresReason(action) && string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult.Failure("Please provide a reason.");
        }

        verification.Status = VerificationState.Apply(action);
        verification.LastAction = action;
        verification.ReviewedAtUtc = DateTime.UtcNow;
        verification.ReviewedByUserId = reviewerId;
        verification.RejectionReason = action == VerificationAction.Approve ? null : reason?.Trim();
        verification.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();

        repository.Update(verification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Verification {VerificationId} reviewed: {Action} by {ReviewerId}", verificationId, action, reviewerId);

        if (action == VerificationAction.Approve)
        {
            await SyncApprovedProfilePhotoAsync(verification);
        }

        var (reviewTitle, reviewMessage) = action switch
        {
            VerificationAction.Approve => NotificationTemplates.VerificationApproved(),
            VerificationAction.Reject => NotificationTemplates.VerificationRejected(verification.RejectionReason ?? "Not specified."),
            VerificationAction.RequestResubmission => NotificationTemplates.ResubmissionRequested(verification.RejectionReason ?? "Not specified."),
            _ => (string.Empty, string.Empty),
        };

        if (!string.IsNullOrEmpty(reviewTitle))
        {
            await _notificationService.CreateAsync(verification.UserId, NotificationType.Verification, reviewTitle, reviewMessage, VerificationLink, createdBy: reviewerId);
        }

        return ServiceResult.Success();
    }

    /// <summary>
    /// On approval, the seller's Profile Photo becomes their public marketplace avatar.
    /// It has to move roots — <see cref="StudentVerification.ProfilePhotoImagePath"/>
    /// lives under the private upload root (never a public URL; see
    /// <see cref="IImageStorageService.SavePrivateAsync"/>), while
    /// <see cref="ApplicationUser.ProfilePicturePath"/> needs a normal servable static-file
    /// URL. Rather than extending <see cref="IImageStorageService"/>'s public contract
    /// with a "copy private to public" method only this one caller would ever use, this
    /// resolves the private physical path via the already-exposed
    /// <see cref="IImageStorageService.GetPrivatePhysicalPath"/> and does a plain file
    /// copy into the exact "uploads/{subfolder}/{guid}{ext}" layout
    /// <see cref="IImageStorageService.SaveAsync"/> itself produces, so the result is an
    /// ordinary public upload indistinguishable from any other. Best-effort: failure here
    /// never fails the approval itself (the reviewer already committed it) — just logs
    /// and leaves the seller's prior/blank avatar in place.
    /// </summary>
    private async Task SyncApprovedProfilePhotoAsync(StudentVerification verification)
    {
        if (string.IsNullOrEmpty(verification.ProfilePhotoImagePath))
        {
            return;
        }

        try
        {
            var privatePhysicalPath = _imageStorageService.GetPrivatePhysicalPath(verification.ProfilePhotoImagePath);
            if (privatePhysicalPath is null)
            {
                return;
            }

            var user = await _userManager.FindByIdAsync(verification.UserId);
            if (user is null)
            {
                return;
            }

            var extension = Path.GetExtension(privatePhysicalPath);
            var targetDirectory = Path.Combine(_environment.WebRootPath, "uploads", ProfileConstants.PhotoStorageSubfolder);
            Directory.CreateDirectory(targetDirectory);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            File.Copy(privatePhysicalPath, Path.Combine(targetDirectory, fileName));

            user.ProfilePicturePath = $"/uploads/{ProfileConstants.PhotoStorageSubfolder}/{fileName}";
            await _userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync approved profile photo to public storage for user {UserId}", verification.UserId);
        }
    }

    public async Task<ServiceResult> UpdateSellingCategoriesAsync(string userId, List<SellingCategory> categories)
    {
        var repository = _unitOfWork.Repository<StudentVerification>();

        // "Current" row = most recent by SubmittedAtUtc — same definition
        // IVerificationQueryService.GetCurrentStatusAsync uses, kept consistent here.
        var page = await repository.GetPagedAsync(
            1,
            1,
            filter: v => v.UserId == userId,
            orderBy: q => q.OrderByDescending(v => v.SubmittedAtUtc));

        var current = page.Items.FirstOrDefault();
        if (current is null || current.Status != VerificationStatus.Verified)
        {
            return ServiceResult.Failure("You must be a verified seller to update your selling categories.");
        }

        current.SellingCategoriesCsv = string.Join(",", categories.Select(c => (int)c));
        repository.Update(current);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private const string VerificationLink = "/Student/Verification";
    private const string AdminVerificationQueueLink = "/Admin/Verifications";
}
