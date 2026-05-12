namespace ClearingHouse.EdiProcessing.Domain.Enums;

/// <summary>
/// Represents the processing status of an EDI file throughout its lifecycle.
/// </summary>
public enum EdiProcessingStatus
{
    /// <summary>The EDI file is pending processing.</summary>
    Pending = 0,

    /// <summary>The EDI file is currently being parsed.</summary>
    Parsing = 1,

    /// <summary>The EDI file segments are being validated.</summary>
    Validating = 2,

    /// <summary>The EDI file segments are being transformed.</summary>
    Transforming = 3,

    /// <summary>The EDI file is being reconciled.</summary>
    Reconciling = 4,

    /// <summary>The EDI file has been fully processed successfully.</summary>
    Completed = 5,

    /// <summary>The EDI file has been partially processed with some errors.</summary>
    PartiallyCompleted = 6,

    /// <summary>The EDI file processing has failed.</summary>
    Failed = 7
}
