using ClearingHouse.SharedKernel.Domain.Entities;
using ClearingHouse.SharedKernel.Domain.ValueObjects;
using ClearingHouse.SftpIngestion.Domain.Enums;

namespace ClearingHouse.SftpIngestion.Domain.Entities;

/// <summary>
/// Entity representing a file that has been ingested from an SFTP endpoint.
/// </summary>
public sealed class IngestedFile : BaseEntity
{
    /// <summary>
    /// Gets the ID of the parent ingestion job.
    /// </summary>
    public Guid IngestionJobId { get; private set; }

    /// <summary>
    /// Gets the original file name from the SFTP endpoint.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// Gets the MD5 content hash for integrity verification.
    /// </summary>
    public string ContentHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the blob storage reference for the uploaded file.
    /// </summary>
    public FileReference? FileReference { get; private set; }

    /// <summary>
    /// Gets the EDI transaction type of the file.
    /// </summary>
    public EdiTransactionType EdiTransactionType { get; private set; } = null!;

    /// <summary>
    /// Gets the current processing status of the file.
    /// </summary>
    public IngestionStatus Status { get; private set; } = IngestionStatus.Idle;

    /// <summary>
    /// Gets the UTC timestamp when the file was ingested.
    /// </summary>
    public DateTime IngestedAt { get; private set; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public CorrelationId CorrelationId { get; private set; } = null!;

    private IngestedFile() { }

    /// <summary>
    /// Creates a new ingested file entity.
    /// </summary>
    /// <param name="ingestionJobId">The parent job ID.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <param name="ediTransactionType">The EDI transaction type.</param>
    /// <param name="correlationId">The correlation ID for tracing.</param>
    /// <returns>A new <see cref="IngestedFile"/> instance.</returns>
    public static IngestedFile Create(
        Guid ingestionJobId,
        string fileName,
        long fileSize,
        EdiTransactionType ediTransactionType,
        CorrelationId correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentOutOfRangeException.ThrowIfNegative(fileSize);
        ArgumentNullException.ThrowIfNull(ediTransactionType);
        ArgumentNullException.ThrowIfNull(correlationId);

        return new IngestedFile
        {
            IngestionJobId = ingestionJobId,
            FileName = fileName,
            FileSize = fileSize,
            EdiTransactionType = ediTransactionType,
            CorrelationId = correlationId,
            Status = IngestionStatus.Downloading,
            IngestedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Records the content hash after file download.
    /// </summary>
    /// <param name="contentHash">The MD5 hash of the file content.</param>
    public void SetContentHash(string contentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ContentHash = contentHash;
    }

    /// <summary>
    /// Sets the blob storage reference after successful upload.
    /// </summary>
    /// <param name="fileReference">The blob storage file reference.</param>
    public void SetFileReference(FileReference fileReference)
    {
        ArgumentNullException.ThrowIfNull(fileReference);
        FileReference = fileReference;
        Status = IngestionStatus.Completed;
    }

    /// <summary>
    /// Marks the file as failed.
    /// </summary>
    public void MarkFailed()
    {
        Status = IngestionStatus.Failed;
    }
}
