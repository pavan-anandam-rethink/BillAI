namespace ClaimLifecycleMonitoring.Domain.Exceptions;

/// <summary>
/// Thrown when an entity requested by identity cannot be found.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    /// <summary>The entity type that could not be located.</summary>
    public string EntityType { get; }

    /// <summary>The identifier used in the lookup.</summary>
    public string Identifier { get; }

    /// <summary>Creates a new <see cref="EntityNotFoundException"/>.</summary>
    public EntityNotFoundException(string entityType, string identifier)
        : base($"{entityType} with identifier '{identifier}' was not found.")
    {
        EntityType = entityType;
        Identifier = identifier;
    }
}
