namespace BillingService.Contracts.Events;

public sealed record BillingOperationCompletedEvent : IntegrationEvent
{
    public BillingOperationCompletedEvent(
        string operationName,
        string aggregateType,
        string aggregateId,
        int accountInfoId,
        int userId,
        string? correlationId = null)
        : base("billing.operation.completed", aggregateType, aggregateId)
    {
        OperationName = operationName;
        AccountInfoId = accountInfoId;
        UserId = userId;
        CorrelationId = correlationId;
    }

    public string OperationName { get; init; }

    public int AccountInfoId { get; init; }

    public int UserId { get; init; }
}
