using SajhaSikshya.DTOs;
using SajhaSikshya.DTOs.Chat;

namespace SajhaSikshya.ViewModels.Chat;

/// <summary>Backs the Chat message-search page. <see cref="Results"/> stays at its default (empty) until a search term is actually submitted — an empty query never runs.</summary>
public class MessageSearchViewModel
{
    public string? SearchTerm { get; set; }

    public PagedResult<MessageSearchResultDto> Results { get; set; } = new();
}
