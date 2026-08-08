using Microsoft.AspNetCore.Http;
using SajhaSikshya.Constants;
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
/// reviewer took.
/// </summary>
public class VerificationService : IVerificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _imageStorageService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IUnitOfWork unitOfWork,
        IImageStorageService imageStorageService,
        INotificationService notificationService,
        ILogger<VerificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _imageStorageService = imageStorageService;
        _notificationService = notificationService;
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

    private const string VerificationLink = "/Student/Verification";
}
