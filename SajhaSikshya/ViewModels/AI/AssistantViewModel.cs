using SajhaSikshya.Services.AI;

namespace SajhaSikshya.ViewModels.AI;

/// <summary>Backs the Marketplace Assistant chat page — the conversation so far (redisplayed on reload, since history lives in Session) plus the fixed suggested-question chips shown when the conversation is empty.</summary>
public class AssistantViewModel
{
    public IReadOnlyList<AssistantMessage> History { get; set; } = Array.Empty<AssistantMessage>();

    public IReadOnlyList<string> SuggestedQuestions { get; set; } = Array.Empty<string>();
}
