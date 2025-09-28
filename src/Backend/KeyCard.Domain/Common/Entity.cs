namespace KeyCard.Domain.Common;

/// <summary>
/// Base type for aggregate entities. Domain is PURE C# (no EF or framework).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
