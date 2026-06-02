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
/// Compatibility facade for payment posting workflows.
/// Preserves the full contract of the existing domain service methods required by
/// the current controllers. Additional methods can be added as needed during migration.
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

    Task<int> UploadFileAsync(
        EraUploadModelWithUserInfo model,
        CancellationToken cancellationToken = default);

    Task<FunderDropdownResponseModel> GetAssignedFundersAsync(
        FunderSearchModelWithUserInfo model,
        CancellationToken cancellationToken = default);

    Task<string> GetNextPaymentIdAsync(
        int accountInfoId,
        CancellationToken cancellationToken = default);
}
