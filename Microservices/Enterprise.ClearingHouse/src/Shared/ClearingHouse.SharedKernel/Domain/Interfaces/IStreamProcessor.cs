using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Domain.Interfaces;

/// <summary>
/// Interface for stream-based file processing, designed for handling large EDI files
/// without loading the entire file into memory.
/// </summary>
public interface IStreamProcessor
{
    /// <summary>
    /// Gets the EDI transaction types this processor can handle.
    /// </summary>
    IReadOnlyCollection<EdiTransactionType> SupportedTypes { get; }

    /// <summary>
    /// Processes a file stream and returns the processing result.
    /// </summary>
    /// <param name="stream">The input file stream.</param>
    /// <param name="transactionType">The EDI transaction type.</param>
    /// <param name="correlationId">The correlation ID for tracing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processing result.</returns>
    Task<StreamProcessingResult> ProcessAsync(
        Stream stream,
        EdiTransactionType transactionType,
        CorrelationId correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a file stream without full processing.
    /// </summary>
    /// <param name="stream">The input file stream.</param>
    /// <param name="transactionType">The EDI transaction type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<StreamValidationResult> ValidateAsync(
        Stream stream,
        EdiTransactionType transactionType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of stream-based file processing.
/// </summary>
/// <param name="IsSuccess">Whether processing completed successfully.</param>
/// <param name="TransactionCount">The number of transactions processed.</param>
/// <param name="ErrorMessages">Any error messages encountered.</param>
/// <param name="ProcessedAt">The UTC timestamp when processing completed.</param>
public sealed record StreamProcessingResult(
    bool IsSuccess,
    int TransactionCount,
    IReadOnlyCollection<string> ErrorMessages,
    DateTime ProcessedAt);

/// <summary>
/// Result of stream validation.
/// </summary>
/// <param name="IsValid">Whether the stream content is valid.</param>
/// <param name="ValidationErrors">Any validation errors found.</param>
public sealed record StreamValidationResult(
    bool IsValid,
    IReadOnlyCollection<string> ValidationErrors);
