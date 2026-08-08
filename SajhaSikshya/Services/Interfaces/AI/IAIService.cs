using SajhaSikshya.Services.AI;

namespace SajhaSikshya.Services.Interfaces.AI;

/// <summary>
/// Single entry point for every Gemini call on the platform — the "Central AI Service"
/// from Phase 10's architectural review. No controller or business service talks to
/// Gemini directly; each AI feature has its own thin feature service (e.g.
/// <c>IListingAIService</c>) that builds a prompt via a prompt builder and calls
/// <see cref="GenerateAsync"/>. Caching, usage logging, input validation, and failure
/// handling all happen once here rather than being duplicated per feature.
/// </summary>
public interface IAIService
{
    Task<ServiceResult<AIGenerationResult>> GenerateAsync(AIGenerationRequest request);
}
