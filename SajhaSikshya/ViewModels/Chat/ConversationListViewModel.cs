using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Chat;

namespace SajhaSikshya.ViewModels.Chat;

/// <summary>Backs the Chat conversation list ("Messages").</summary>
public class ConversationListViewModel
{
    public PagedResult<ConversationDto> Page { get; set; } = new();

    public string? SearchTerm { get; set; }

    public bool IncludeArchived { get; set; }
}
