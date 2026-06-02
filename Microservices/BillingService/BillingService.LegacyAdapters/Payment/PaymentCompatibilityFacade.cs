using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.LegacyAdapters.Payment;

/// <summary>
/// Wraps <see cref="IPaymentPostingService"/> for injection into CQRS handlers and
/// future Application-layer extraction. All calls delegate unchanged to the legacy
/// domain service to ensure zero functional regression.
/// </summary>
public sealed class PaymentCompatibilityFacade(IPaymentPostingService paymentPostingService)
    : IPaymentCompatibilityFacade
{
    public Task<PaymentsResponseModel> GetAllPaymentsAsync(
        GetPaymentsModel model,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.GetAllPayments(model);
    }

    public Task<PaymentSummary> GetPaymentSummaryAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.GetPaymentSummaryAsync(paymentId);
    }

    public Task<int> PostManualPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.PostManualPaymentAsync(paymentId);
    }

    public Task<int> CreateManualPatientPaymentAsync(
        ManualCreatePaymentModel model,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.CreateManualPatientPaymentAsync(model);
    }

    public Task<List<int>> DeletePaymentAsync(
        int[] paymentIds,
        int memberId,
        int accountInfoId,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.DeletePaymentAsync(paymentIds, memberId, accountInfoId);
    }

    public Task<List<int>> ReconcilePaymentAsync(
        int[] paymentIds,
        int memberId,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.ReconcilePaymentAsync(paymentIds, memberId);
    }

    public Task<int> UploadFileAsync(
        EraUploadModelWithUserInfo model,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.UploadFileAsync(model);
    }

    public Task<FunderDropdownResponseModel> GetAssignedFundersAsync(
        FunderSearchModelWithUserInfo model,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.GetAssignedFundersAsync(model);
    }

    public Task<string> GetNextPaymentIdAsync(
        int accountInfoId,
        CancellationToken cancellationToken = default)
    {
        return paymentPostingService.GetNextPaymentID(accountInfoId);
    }
}
