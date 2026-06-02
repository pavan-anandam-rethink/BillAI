using BillingService.Domain.Models;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;

namespace BillingService.LegacyAdapters.Payments;

/// <summary>
/// Application-facing compatibility interface for payment posting operations.
/// Provides a stable seam between CQRS command/query handlers and the legacy
/// <see cref="BillingService.Domain.Interfaces.Payment.IPaymentPostingService"/>.
/// </summary>
public interface IPaymentCompatibilityFacade
{
    Task<PaymentsResponseModel> GetAllPaymentsAsync(
        GetPaymentsModel model,
        CancellationToken cancellationToken = default);

    Task<PaymentSummary> GetPaymentSummaryAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<int> CreateManualPatientPaymentAsync(
        ManualCreatePaymentModel model,
        CancellationToken cancellationToken = default);

    Task<List<int>> DeletePaymentAsync(
        int[] paymentIds,
        int memberId,
        int accountInfoId,
        CancellationToken cancellationToken = default);

    Task<List<int>> ReconcilePaymentAsync(
        int[] paymentIds,
        int memberId,
        CancellationToken cancellationToken = default);

    Task<int> UploadEraFileAsync(
        EraUploadModelWithUserInfo model,
        CancellationToken cancellationToken = default);

    Task<FunderDropdownResponseModel> GetAssignedFundersAsync(
        FunderSearchModelWithUserInfo model,
        CancellationToken cancellationToken = default);

    Task AddUnAllocatedPaymentsAsync(
        UnAllocatedPaymentsModel model,
        CancellationToken cancellationToken = default);

    Task<UnAllocatedPaymentsModel> GetUnAllocatedPaymentsByIdAsync(
        UnAllocatedPaymentRequestModel model,
        CancellationToken cancellationToken = default);
}
