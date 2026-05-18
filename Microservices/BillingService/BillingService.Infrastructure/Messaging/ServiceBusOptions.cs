namespace BillingService.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "BillingService:ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string TopicName { get; init; } = "billing-events";
}
