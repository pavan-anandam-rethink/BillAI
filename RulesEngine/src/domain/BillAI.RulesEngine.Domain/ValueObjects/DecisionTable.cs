namespace BillAI.RulesEngine.Domain.ValueObjects;

/// <summary>
/// Represents a decision table with input/output columns and condition rows.
/// </summary>
public sealed class DecisionTable
{
    /// <summary>Gets the columns that define input criteria.</summary>
    public IReadOnlyList<DecisionTableColumn> InputColumns { get; init; } = [];

    /// <summary>Gets the columns that define output actions/values.</summary>
    public IReadOnlyList<DecisionTableColumn> OutputColumns { get; init; } = [];

    /// <summary>Gets the rows of condition-output mappings.</summary>
    public IReadOnlyList<DecisionTableRow> Rows { get; init; } = [];

    /// <summary>
    /// When true, evaluation stops at the first matching row.
    /// When false, all matching rows are returned.
    /// </summary>
    public bool HitPolicyFirst { get; init; } = true;
}

/// <summary>
/// Represents a column definition in a decision table.
/// </summary>
public sealed class DecisionTableColumn
{
    public string Name { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public string DataType { get; init; } = "string";
}

/// <summary>
/// Represents a row of conditions and their corresponding outputs in a decision table.
/// </summary>
public sealed class DecisionTableRow
{
    /// <summary>Gets the condition cells keyed by input column name.</summary>
    public IReadOnlyDictionary<string, object?> Conditions { get; init; }
        = new Dictionary<string, object?>();

    /// <summary>Gets the output cells keyed by output column name.</summary>
    public IReadOnlyDictionary<string, object?> Outputs { get; init; }
        = new Dictionary<string, object?>();
}
