namespace ClearingHouse.EdiProcessing.Domain.Enums;

/// <summary>
/// Represents the severity level of a validation result.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Informational message, no action required.</summary>
    Information = 0,

    /// <summary>Warning that may require attention.</summary>
    Warning = 1,

    /// <summary>Error that must be addressed.</summary>
    Error = 2,

    /// <summary>Critical error that prevents further processing.</summary>
    Critical = 3
}
