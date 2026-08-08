using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Enums;

/// <summary>The kind of content a <see cref="Entities.Chat.Message"/> carries.</summary>
public enum MessageType
{
    [Display(Name = "Text")]
    Text = 0,

    [Display(Name = "Image")]
    Image = 1,

    [Display(Name = "File")]
    File = 2,

    [Display(Name = "System")]
    System = 3,
}
