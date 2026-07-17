namespace BillAI.RulesEngine.Shared.Abstractions;

/// <summary>
/// Marker interface for all domain entities with a typed identifier.
/// </summary>
public interface IEntity<TId>
{
    TId Id { get; }
}
