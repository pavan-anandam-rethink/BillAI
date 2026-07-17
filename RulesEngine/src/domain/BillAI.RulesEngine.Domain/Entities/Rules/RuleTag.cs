namespace BillAI.RulesEngine.Domain.Entities.Rules;

/// <summary>
/// Represents a tag associated with a <see cref="RuleDefinition"/>.
/// </summary>
public sealed class RuleTag
{
    public RuleTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tag value cannot be empty.", nameof(value));
        Value = value.Trim().ToLowerInvariant();
    }

    public string Value { get; }
}
