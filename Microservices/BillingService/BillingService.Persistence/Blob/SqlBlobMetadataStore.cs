using BillingService.Application.Abstractions.Blob;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Persistence.Blob;

public sealed class SqlBlobMetadataStore(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<SqlBlobMetadataStore> logger)
    : IBlobMetadataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(BlobMetadata metadata, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = new SqlCommand(@"
            INSERT INTO billing.BlobMetadata
                (CorrelationId, BlobName, ContainerName, Tags, CreatedOnUtc)
            VALUES
                (@CorrelationId, @BlobName, @ContainerName, @Tags, @CreatedOnUtc)", connection);

        command.Parameters.AddWithValue("@CorrelationId", metadata.CorrelationId);
        command.Parameters.AddWithValue("@BlobName", metadata.BlobName);
        command.Parameters.AddWithValue("@ContainerName", metadata.ContainerName);
        command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(metadata.Tags, SerializerOptions));
        command.Parameters.AddWithValue("@CreatedOnUtc", metadata.CreatedOnUtc);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Saved blob metadata for correlation {CorrelationId} blob {BlobName}",
            metadata.CorrelationId,
            metadata.BlobName);
    }

    public async Task<BlobMetadata?> FindByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = new SqlCommand(@"
            SELECT TOP 1 CorrelationId, BlobName, ContainerName, Tags, CreatedOnUtc
            FROM billing.BlobMetadata
            WHERE CorrelationId = @CorrelationId
            ORDER BY CreatedOnUtc DESC", connection);

        command.Parameters.AddWithValue("@CorrelationId", correlationId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadBlobMetadata(reader);
    }

    public async Task<IReadOnlyList<BlobMetadata>> FindByTagAsync(
        string tagKey,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = new SqlCommand(@"
            SELECT CorrelationId, BlobName, ContainerName, Tags, CreatedOnUtc
            FROM billing.BlobMetadata
            WHERE JSON_VALUE(Tags, '$.' + @TagKey) = @TagValue
            ORDER BY CreatedOnUtc DESC", connection);

        command.Parameters.AddWithValue("@TagKey", tagKey);
        command.Parameters.AddWithValue("@TagValue", tagValue);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var results = new List<BlobMetadata>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(ReadBlobMetadata(reader));
        }

        return results;
    }

    private static BlobMetadata ReadBlobMetadata(SqlDataReader reader)
    {
        var tags = new Dictionary<string, string>();
        var tagsJson = reader["Tags"] as string;
        if (!string.IsNullOrWhiteSpace(tagsJson))
        {
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson, SerializerOptions);
            if (deserialized is not null)
            {
                foreach (var (key, value) in deserialized)
                {
                    tags[key] = value;
                }
            }
        }

        return new BlobMetadata(
            blobName: (string)reader["BlobName"],
            containerName: (string)reader["ContainerName"],
            correlationId: (string)reader["CorrelationId"])
        {
            CreatedOnUtc = (DateTimeOffset)reader["CreatedOnUtc"],
            Tags = tags
        };
    }
}
