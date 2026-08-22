using System.ComponentModel.DataAnnotations.Schema;

namespace InstituteManagement.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("CreatedAtUtc")]
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
