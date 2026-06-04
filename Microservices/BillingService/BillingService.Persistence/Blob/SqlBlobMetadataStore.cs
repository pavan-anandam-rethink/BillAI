using BillingService.Application.Abstractions.Blob;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BillingService.Persistence.Blob;

/// <summary>
/// Persists and retrieves lightweight blob operational metadata using SQL Server.
/// Only BlobName, ContainerName, and CorrelationId are stored.
/// No file paths, blob URLs, or physical storage references are ever written to this table.
/// Blob Storage itself remains the authoritative source of all artifact content.
/// </summary>
public sealed class SqlBlobMetadataStore(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<SqlBlobMetadataStore> logger)
    : IBlobMetadataStore
{
    private const string InsertSql =
        """
        INSERT INTO billing.BillingBlobMetadata
            (Id, BlobName, ContainerName, CorrelationId, CreatedOnUtc)
        SELECT NEWID(), @BlobName, @ContainerName, @CorrelationId, SYSUTCDATETIME()
        WHERE NOT EXISTS (
            SELECT 1 FROM billing.BillingBlobMetadata
            WHERE BlobName = @BlobName AND ContainerName = @ContainerName
        );
        """;

    private const string SelectByCorrelationIdSql =
        """
        SELECT BlobName, ContainerName, CorrelationId
        FROM billing.BillingBlobMetadata
        WHERE CorrelationId = @CorrelationId;
        """;

    private const string SelectByBlobNameSql =
        """
        SELECT TOP 1 BlobName, ContainerName, CorrelationId
        FROM billing.BillingBlobMetadata
        WHERE BlobName = @BlobName AND ContainerName = @ContainerName;
        """;

    public async Task SaveAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(InsertSql, connection);
        command.Parameters.Add("@BlobName", SqlDbType.NVarChar, 1024).Value = metadata.BlobName;
        command.Parameters.Add("@ContainerName", SqlDbType.NVarChar, 256).Value = metadata.ContainerName;
        command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar, 128).Value =
            (object?)metadata.CorrelationId ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug(
            "Saved blob metadata for {BlobName} in {ContainerName} with correlationId {CorrelationId}",
            metadata.BlobName,
            metadata.ContainerName,
            metadata.CorrelationId);
    }

    public async Task<IReadOnlyList<BlobMetadata>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var results = new List<BlobMetadata>();

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(SelectByCorrelationIdSql, connection);
        command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar, 128).Value = correlationId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new BlobMetadata(
                blobName: reader.GetString(0),
                containerName: reader.GetString(1),
                correlationId: reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        return results;
    }

    public async Task<BlobMetadata?> GetByBlobNameAsync(
        string blobName,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(SelectByBlobNameSql, connection);
        command.Parameters.Add("@BlobName", SqlDbType.NVarChar, 1024).Value = blobName;
        command.Parameters.Add("@ContainerName", SqlDbType.NVarChar, 256).Value = containerName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new BlobMetadata(
                blobName: reader.GetString(0),
                containerName: reader.GetString(1),
                correlationId: reader.IsDBNull(2) ? null : reader.GetString(2));
        }

        return null;
    }
}
