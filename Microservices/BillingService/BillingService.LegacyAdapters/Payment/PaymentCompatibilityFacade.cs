using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;

namespace BillingService.LegacyAdapters.Payment;

/// <summary>
/// Delegates every call to the underlying <see cref="IPaymentPostingService"/>.
/// Preserves 100% of the existing domain behavior while exposing a cancellation-token
/// aware, testable surface that new CQRS handlers and controllers can depend on.
/// </summary>
public sealed class PaymentCompatibilityFacade(IPaymentPostingService paymentPostingService)
    : IPaymentCompatibilityFacade
{
    public Task<PaymentsResponseModel> GetAllPaymentsAsync(
        GetPaymentsModel model,
        CancellationToken cancellationToken = default)
        => paymentPostingService.GetAllPayments(model);

    public Task<PaymentSummary> GetPaymentSummaryAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
        => paymentPostingService.GetPaymentSummaryAsync(paymentId);

    public Task<int> PostManualPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
        => paymentPostingService.PostManualPaymentAsync(paymentId);

    public Task<int> CreateManualPatientPaymentAsync(
        ManualCreatePaymentModel model,
        CancellationToken cancellationToken = default)
        => paymentPostingService.CreateManualPatientPaymentAsync(model);

    public Task<List<int>> DeletePaymentAsync(
        int[] paymentIds,
        int memberId,
        int accountInfoId,
        CancellationToken cancellationToken = default)
        => paymentPostingService.DeletePaymentAsync(paymentIds, memberId, accountInfoId);

    public Task<List<int>> ReconcilePaymentAsync(
        int[] paymentIds,
        int memberId,
        CancellationToken cancellationToken = default)
        => paymentPostingService.ReconcilePaymentAsync(paymentIds, memberId);

    public Task<EOBPaymentInfo> GetEOBPaymentInfoAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
        => paymentPostingService.GetEOBPaymentInfoAsync(paymentId);

    public Task<string> GetNextPaymentIdAsync(
        int accountInfoId,
        CancellationToken cancellationToken = default)
        => paymentPostingService.GetNextPaymentID(accountInfoId);

    public Task<int> UploadEraFileAsync(
        EraUploadModelWithUserInfo model,
        CancellationToken cancellationToken = default)
        => paymentPostingService.UploadFileAsync(model);

    public Task<FunderDropdownResponseModel> GetAssignedFundersAsync(
        FunderSearchModelWithUserInfo model,
        CancellationToken cancellationToken = default)
        => paymentPostingService.GetAssignedFundersAsync(model);

    public Task AddUnAllocatedPaymentsAsync(
        UnAllocatedPaymentsModel model,
        CancellationToken cancellationToken = default)
        => paymentPostingService.AddUnAllocatedPayments(model);

    public Task<UnAllocatedPaymentsModel> GetUnAllocatedPaymentsByIdAsync(
        UnAllocatedPaymentRequestModel model,
        CancellationToken cancellationToken = default)
        => paymentPostingService.GetUnAllocatedPaymentsById(model);
}
