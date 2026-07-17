using BillAI.RulesEngine.Domain.Enums;

namespace BillAI.RulesEngine.Domain.ValueObjects;

/// <summary>
/// Represents a single condition in a rule expression tree.
/// Supports recursive nesting for AND/OR/NOT composites.
/// </summary>
public sealed class RuleCondition
{
    /// <summary>Field or property path to evaluate (e.g. "claim.totalAmount").</summary>
    public string? Field { get; init; }

    /// <summary>The comparison operator.</summary>
    public ConditionOperator Operator { get; init; }

    /// <summary>The right-hand side value for comparison.</summary>
    public object? Value { get; init; }

    /// <summary>The data type of the comparison.</summary>
    public DataType DataType { get; init; } = DataType.String;

    /// <summary>Logical combinator for composite conditions.</summary>
    public LogicalOperator? LogicalOperator { get; init; }

    /// <summary>Child conditions when using a logical combinator.</summary>
    public IReadOnlyList<RuleCondition> Children { get; init; } = [];

    /// <summary>Optional function name when OperandType is Function.</summary>
    public string? FunctionName { get; init; }

    /// <summary>Returns true when this node is a composite (AND/OR/NOT) group.</summary>
    public bool IsComposite => LogicalOperator.HasValue && Children.Count > 0;
}
