using System.ComponentModel.DataAnnotations;

namespace SajhaSikshya.Data.Entities;

/// <summary>
/// Common audit and soft-delete fields shared by every domain entity in the system.
/// Inheriting from this base keeps auditing behavior consistent without repeating
/// the same four properties (and their EF Core configuration) on every entity.
/// </summary>
public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Soft-delete flag. Entities are never physically removed from the database;
    /// repositories filter these out by default so audit history is preserved.
    /// </summary>
    public bool IsDeleted { get; set; }
}
