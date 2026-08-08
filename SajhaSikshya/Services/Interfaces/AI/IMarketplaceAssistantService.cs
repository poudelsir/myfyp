using SajhaSikshya.Services.AI;

namespace SajhaSikshya.Services.Interfaces.AI;

/// <summary>Feature-facing AI service for the Marketplace Assistant — the thin layer between the root-level AssistantController and the central <see cref="IAIService"/>.</summary>
public interface IMarketplaceAssistantService
{
    Task<ServiceResult<string>> AskAsync(string userId, string role, string question, IReadOnlyList<AssistantMessage> history);
}
