using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SftpIngestion.Domain.Interfaces;

/// <summary>
/// Orchestrates the SFTP ingestion workflow for a given clearinghouse.
/// </summary>
public interface IIngestionOrchestrator
{
    /// <summary>
    /// Executes the full ingestion workflow: poll, download, upload, and publish events.
    /// </summary>
    /// <param name="clearinghouseIdentifier">The clearinghouse to poll.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created ingestion job.</returns>
    Task<Guid> ExecuteIngestionAsync(
        ClearinghouseIdentifier clearinghouseIdentifier,
        CancellationToken cancellationToken = default);
}
