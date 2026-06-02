namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Blob-first metadata tracking contract.
///
/// The database MUST NOT store blob file paths or physical storage references.
/// Instead, this interface provides a lightweight record of the correlation
/// context, classification tags, and immutable processing state for every blob
/// artifact.  Blob Storage itself remains the authoritative source of content.
/// </summary>
public interface IBlobMetadataStore
{
    /// <summary>
    /// Records that a new blob artifact has been produced. The metadata is
    /// persisted in the operational store for audit and correlation purposes.
    /// No file path or URL is stored.
    /// </summary>
    Task RecordAsync(BlobMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a previously recorded blob artifact as successfully processed.
    /// </summary>
    Task MarkProcessedAsync(
        Guid metadataId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a previously recorded blob artifact as failed, storing the
    /// reason for observability and retry decisions.
    /// </summary>
    Task MarkFailedAsync(
        Guid metadataId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns lightweight metadata entries for a given correlation ID.
    /// Results are sorted by <see cref="BlobMetadata.OccurredOnUtc"/> descending.
    /// The caller must use the <see cref="BlobMetadata.BlobName"/> to retrieve
    /// the actual content directly from Blob Storage.
    /// </summary>
    Task<IReadOnlyList<BlobMetadata>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns lightweight metadata entries for a given account and blob category.
    /// </summary>
    Task<IReadOnlyList<BlobMetadata>> GetByAccountAndCategoryAsync(
        int accountInfoId,
        string category,
        CancellationToken cancellationToken = default);
}
