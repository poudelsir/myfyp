using Microsoft.AspNetCore.Http;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.Catalog;
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

        var university = await _unitOfWork.Repository<University>().GetByIdAsync(model.UniversityId);
        if (university is null || !university.IsActive)
        {
            return ServiceResult<int>.Failure("The selected university does not exist.");
        }

        if (model.StudentIdImage is null)
        {
            return ServiceResult<int>.Failure("Please upload a photo of your student ID.");
        }

        // SavePrivateAsync (not SaveAsync) — verification documents are stored outside
        // wwwroot entirely, never reachable by a public URL. See IImageStorageService's
        // remarks and VerificationImagesController for the authorized read path.
        var saveResult = await _imageStorageService.SavePrivateAsync(
            model.StudentIdImage,
            VerificationConstants.ImageStorageSubfolder,
            VerificationConstants.AllowedImageExtensions,
            VerificationConstants.MaximumImageSizeMB);

        if (!saveResult.Succeeded)
        {
            return ServiceResult<int>.Failure(saveResult.Errors.FirstOrDefault() ?? "The student ID photo could not be uploaded.");
        }

        var verification = new StudentVerification
        {
            UserId = userId,
            UniversityId = model.UniversityId,
            StudentNumber = model.StudentNumber.Trim(),
            StudentIdImagePath = saveResult.Data!,
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
            // No database row was written — the uploaded file is now orphaned, so it's
            // removed rather than left behind. Mirrors ListingService.UploadImagesAsync's
            // save-then-rollback-on-failure shape.
            _logger.LogError(ex, "Failed to save verification submission for user {UserId}", userId);
            await _imageStorageService.DeletePrivateAsync(saveResult.Data!);
            return ServiceResult<int>.Failure("Failed to submit your verification. Please try again.");
        }

        var (submittedTitle, submittedMessage) = NotificationTemplates.VerificationSubmitted();
        await _notificationService.CreateAsync(userId, NotificationType.Verification, submittedTitle, submittedMessage, VerificationLink, createdBy: userId);

        return ServiceResult<int>.Success(verification.Id);
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
