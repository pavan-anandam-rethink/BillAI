using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;

namespace BillingService.LegacyAdapters.Payment;

/// <summary>
/// Anti-corruption facade that exposes <see cref="IPaymentPostingService"/> through a
/// compatibility interface. Controllers and future CQRS handlers call this facade rather
/// than the legacy service directly, preserving the existing domain behavior while adding
/// a stable, testable seam between the API layer and the legacy implementation.
/// </summary>
public interface IPaymentCompatibilityFacade
{
    Task<PaymentsResponseModel> GetAllPaymentsAsync(
        GetPaymentsModel model,
        CancellationToken cancellationToken = default);

    Task<PaymentSummary> GetPaymentSummaryAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<int> PostManualPaymentAsync(
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

    Task<EOBPaymentInfo> GetEOBPaymentInfoAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<string> GetNextPaymentIdAsync(
        int accountInfoId,
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
