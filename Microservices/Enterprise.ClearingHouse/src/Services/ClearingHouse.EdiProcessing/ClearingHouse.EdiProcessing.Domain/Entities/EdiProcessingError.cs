using ClearingHouse.EdiProcessing.Domain.Enums;
using ClearingHouse.SharedKernel.Domain.Entities;

namespace ClearingHouse.EdiProcessing.Domain.Entities;

/// <summary>
/// Represents an error encountered during EDI file processing.
/// </summary>
public class EdiProcessingError : BaseEntity
{
    /// <summary>Gets the identifier of the parent EDI file.</summary>
    public Guid EdiFileId { get; private set; }

    /// <summary>Gets the segment sequence number where the error occurred, if applicable.</summary>
    public int? SegmentSequence { get; private set; }

    /// <summary>Gets the error code.</summary>
    public string ErrorCode { get; private set; } = string.Empty;

    /// <summary>Gets the error message.</summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>Gets the severity of the error.</summary>
    public ValidationSeverity Severity { get; private set; }

    /// <summary>Gets a value indicating whether the error is recoverable.</summary>
    public bool IsRecoverable { get; private set; }

    /// <summary>
    /// Creates a new <see cref="EdiProcessingError"/> instance.
    /// </summary>
    /// <param name="ediFileId">The parent EDI file identifier.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="severity">The error severity.</param>
    /// <param name="isRecoverable">Whether the error is recoverable.</param>
    /// <param name="segmentSequence">The segment sequence number, if applicable.</param>
    /// <returns>A new <see cref="EdiProcessingError"/> instance.</returns>
    public static EdiProcessingError Create(
        Guid ediFileId,
        string errorCode,
        string errorMessage,
        ValidationSeverity severity,
        bool isRecoverable,
        int? segmentSequence = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new EdiProcessingError
        {
            Id = Guid.NewGuid(),
            EdiFileId = ediFileId,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            Severity = severity,
            IsRecoverable = isRecoverable,
            SegmentSequence = segmentSequence,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private EdiProcessingError() { }
}
