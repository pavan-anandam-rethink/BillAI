using ClearingHouse.SharedKernel.Domain.Events;
using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SftpIngestion.Domain.Events;

/// <summary>
/// Domain event raised when a new ingestion job is created.
/// </summary>
public sealed record IngestionJobCreatedEvent : IDomainEvent
{
    /// <summary>Gets the ingestion job ID.</summary>
    public Guid JobId { get; }

    /// <summary>Gets the clearinghouse identifier.</summary>
    public ClearinghouseIdentifier ClearinghouseIdentifier { get; }

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionJobCreatedEvent"/> record.
    /// </summary>
    public IngestionJobCreatedEvent(Guid jobId, ClearinghouseIdentifier clearinghouseIdentifier, CorrelationId correlationId)
    {
        JobId = jobId;
        ClearinghouseIdentifier = clearinghouseIdentifier;
        CorrelationId = correlationId;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event raised when an ingestion job completes successfully.
/// </summary>
public sealed record IngestionJobCompletedEvent : IDomainEvent
{
    /// <summary>Gets the ingestion job ID.</summary>
    public Guid JobId { get; }

    /// <summary>Gets the clearinghouse identifier.</summary>
    public ClearinghouseIdentifier ClearinghouseIdentifier { get; }

    /// <summary>Gets the number of files processed.</summary>
    public int FilesProcessed { get; }

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionJobCompletedEvent"/> record.
    /// </summary>
    public IngestionJobCompletedEvent(Guid jobId, ClearinghouseIdentifier clearinghouseIdentifier, int filesProcessed, CorrelationId correlationId)
    {
        JobId = jobId;
        ClearinghouseIdentifier = clearinghouseIdentifier;
        FilesProcessed = filesProcessed;
        CorrelationId = correlationId;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event raised when an ingestion job fails.
/// </summary>
public sealed record IngestionJobFailedEvent : IDomainEvent
{
    /// <summary>Gets the ingestion job ID.</summary>
    public Guid JobId { get; }

    /// <summary>Gets the clearinghouse identifier.</summary>
    public ClearinghouseIdentifier ClearinghouseIdentifier { get; }

    /// <summary>Gets the error message.</summary>
    public string ErrorMessage { get; }

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionJobFailedEvent"/> record.
    /// </summary>
    public IngestionJobFailedEvent(Guid jobId, ClearinghouseIdentifier clearinghouseIdentifier, string errorMessage, CorrelationId correlationId)
    {
        JobId = jobId;
        ClearinghouseIdentifier = clearinghouseIdentifier;
        ErrorMessage = errorMessage;
        CorrelationId = correlationId;
        OccurredOn = DateTime.UtcNow;
    }
}
