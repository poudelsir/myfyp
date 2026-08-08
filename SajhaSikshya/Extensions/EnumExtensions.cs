using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SajhaSikshya.Extensions;

/// <summary>
/// Reflection-based helpers for reading <see cref="DisplayAttribute"/> off enum
/// members, so UI-facing text (labels, dropdown options, tooltips) lives in one
/// place next to the enum definition instead of being duplicated with switch
/// statements at every call site (views, DTOs, chart labels, AI prompt text, etc).
/// Results are cached since these are looked up repeatedly when rendering lists.
/// </summary>
public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<(Type EnumType, string Name), DisplayAttribute?> DisplayAttributeCache = new();

    /// <summary>The [Display(Name = ...)] text for this value, or its raw member name if none is set.</summary>
    public static string GetDisplayName(this Enum value) =>
        GetDisplayAttribute(value)?.Name ?? value.ToString();

    /// <summary>The [Display(Description = ...)] text for this value, falling back to <see cref="GetDisplayName"/>.</summary>
    public static string GetDescription(this Enum value) =>
        GetDisplayAttribute(value)?.Description ?? value.GetDisplayName();

    /// <summary>
    /// Builds a dropdown-ready option list for <typeparamref name="TEnum"/>, in declaration
    /// order, using each member's <see cref="GetDisplayName"/> as the visible text and its
    /// name as the option value (so ASP.NET Core's default model binder round-trips it back
    /// to the enum by name regardless of how the value is stored in the database).
    /// </summary>
    public static IEnumerable<SelectListItem> GetSelectList<TEnum>(TEnum? selected = null) where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(value => new SelectListItem
            {
                Text = ((Enum)value).GetDisplayName(),
                Value = value.ToString(),
                Selected = selected.HasValue && EqualityComparer<TEnum>.Default.Equals(selected.Value, value),
            })
            .ToList();
    }

    private static DisplayAttribute? GetDisplayAttribute(Enum value)
    {
        var type = value.GetType();
        var name = value.ToString();

        return DisplayAttributeCache.GetOrAdd((type, name), key =>
            key.EnumType.GetMember(key.Name).FirstOrDefault()?.GetCustomAttribute<DisplayAttribute>());
    }
}
