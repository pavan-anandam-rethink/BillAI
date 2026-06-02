using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;

namespace BillingService.LegacyAdapters.Payments;

/// <summary>
/// Wraps <see cref="IPaymentPostingService"/> as a compatibility facade.
/// All existing payment-posting behavior is preserved by delegation —
/// no business logic is added or removed here.
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
