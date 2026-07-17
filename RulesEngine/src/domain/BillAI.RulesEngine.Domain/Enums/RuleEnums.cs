namespace BillAI.RulesEngine.Domain.Enums;

/// <summary>Lifecycle status of a rule definition.</summary>
public enum RuleStatus
{
    /// <summary>Rule is in draft and not yet active.</summary>
    Draft = 0,

    /// <summary>Rule is published and available for execution.</summary>
    Published = 1,

    /// <summary>Rule has been archived and is no longer active.</summary>
    Archived = 2
}

/// <summary>The logical type of a rule condition.</summary>
public enum ConditionOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    In,
    NotIn,
    IsNull,
    IsNotNull,
    Regex,
    Between
}

/// <summary>Logical combinator for grouping conditions.</summary>
public enum LogicalOperator
{
    And,
    Or,
    Not
}

/// <summary>The type of operand in a rule condition.</summary>
public enum OperandType
{
    Field,
    Constant,
    Function
}

/// <summary>The data type used for comparisons.</summary>
public enum DataType
{
    String,
    Numeric,
    Boolean,
    Date,
    DateTime,
    Array,
    Json
}

/// <summary>Severity level of a rule evaluation result.</summary>
public enum RuleSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
