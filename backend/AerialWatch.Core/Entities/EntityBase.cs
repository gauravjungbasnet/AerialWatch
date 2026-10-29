namespace AerialWatch.Core.Entities;

/// <summary>Base for persisted entities: identity and audit timestamps.</summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
