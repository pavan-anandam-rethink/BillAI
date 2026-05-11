using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace Reconciliation.Application.Commands.ReconcilePayment;

public record ReconcilePaymentCommand : IRequest<Result>
{
    public string ClaimId { get; init; } = string.Empty;
    public decimal PaidAmount { get; init; }
    public decimal AdjustmentAmount { get; init; }
    public DateTime PaymentDate { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
