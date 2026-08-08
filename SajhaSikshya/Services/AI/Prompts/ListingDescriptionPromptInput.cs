using SajhaSikshya.Data.Enums;

namespace SajhaSikshya.Services.AI.Prompts;

/// <summary>Everything <see cref="ListingDescriptionPromptBuilder"/> needs — resolved server-side from CategoryId/SubjectId before building the prompt, never trusted as free-text from the client beyond <see cref="Title"/> itself.</summary>
public record ListingDescriptionPromptInput(
    string? Title,
    BookCondition Condition,
    string CategoryName,
    string SubjectName,
    string AcademicLevelName,
    decimal PriceAmount,
    bool IsDonation);
