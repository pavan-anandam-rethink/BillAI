namespace BillingService.Application.Common.Configuration;

public sealed class ModernizationFeatureFlags
{
    public const string SectionName = "BillingService:Modernization";

    /// <summary>
    /// When true, registers the Clean Architecture application layer adapters and legacy facades.
    /// </summary>
    public bool EnableCleanArchitectureAdapters { get; init; }

    /// <summary>
    /// When true, routes cache reads/writes through the Redis-backed IBillingCache decorator.
    /// Requires a valid Redis connection string in BillingService:Redis:Configuration.
    /// </summary>
    public bool EnableDistributedCacheDecorators { get; init; }

    /// <summary>
    /// When true, activates the OutboxPublisherWorker background service, which polls the
    /// durable outbox table and publishes integration events to Azure Service Bus.
    /// Requires EnableOutboxPersistence = true and a valid Service Bus connection string.
    /// </summary>
    public bool EnableOutboxPublisher { get; init; }

    /// <summary>
    /// When true, activates the durable outbox write path (OutboxPersistenceWriter) and
    /// the SQL read path (SqlOutboxPoller). Requires the billing.OutboxMessages table to exist.
    /// </summary>
    public bool EnableOutboxPersistence { get; init; }

    /// <summary>
    /// When true, routes select read-heavy operations through optimized CQRS read-model queries
    /// using AsNoTracking DTO projections instead of the legacy service layer.
    /// </summary>
    public bool EnableReadModelQueries { get; init; }

    /// <summary>
    /// When true, activates the Blob-First metadata store (IBlobMetadataStore) backed by
    /// Azure Blob Storage tags and blob properties. No file paths or URIs are stored in SQL.
    /// </summary>
    public bool EnableBlobFirstArchitecture { get; init; }

    /// <summary>
    /// When true, activates OpenTelemetry distributed tracing in the Infrastructure layer.
    /// </summary>
    public bool EnableOpenTelemetry { get; init; } = true;
}
