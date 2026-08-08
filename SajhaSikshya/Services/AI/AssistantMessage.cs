namespace SajhaSikshya.Services.AI;

/// <summary>
/// One turn in a Marketplace Assistant conversation. Shared by session storage
/// (<see cref="Extensions.SessionExtensions"/>), <see cref="Prompts.MarketplaceAssistantPromptBuilder"/>
/// (formats the recent history into the prompt), and the Assistant page's view model —
/// one shape, no duplicate "chat message" type per layer.
/// </summary>
/// <param name="Role">"user" or "assistant".</param>
public record AssistantMessage(string Role, string Text);
