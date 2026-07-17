namespace BillAI.RulesEngine.Shared.Abstractions;

/// <summary>
/// Represents an auditable entity that records creation and modification timestamps and users.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    string? CreatedBy { get; }
    DateTimeOffset? UpdatedAt { get; }
    string? UpdatedBy { get; }
}
