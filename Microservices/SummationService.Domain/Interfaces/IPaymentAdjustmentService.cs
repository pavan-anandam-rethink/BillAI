using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.ReportingModels;

namespace SummationService.Domain.Interfaces;

public interface IPaymentAdjustmentService
{
    Task<int?> FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
    Task<PaymentsAdjustmentsEntity?> GetPaymentsAdjustmentsByIdAsync(ClaimTransactionType transactionType, int claimId, int transactionTypeId, CancellationToken cancellationToken);
    Task<PaymentsAdjustmentsEntity?> PreparePaymentsAdjustmentsAsync(ClaimTransactionType transactionType, ClaimEntity claim, int transactionTypeId, CancellationToken cancellationToken);
    Task<ClaimEntity?> GetClaimByIdAsync(int claimId, CancellationToken cancellationToken);
    //Update includes soft-delete
    Task<int> UpdatePaymentsAdjustmentsAsync(PaymentsAdjustmentsEntity paymentsAdjustmentsEntity, CancellationToken cancellationToken);
    Task<bool> AddOrUpdatePaymentAdjustmentAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
    Task<PaymentsAdjustmentsEntity?> PreparePaymentsAdjustmentsListAsync(ClaimTransactionType transactionType, PaymentsAdjustmentsEntity paymentsAdjustments, PaymentEntity payment);
    Task<List<PaymentsAdjustmentsEntity?>> GetPaymentsAdjustmentsListByClaimIdAsync(int claimId, CancellationToken cancellationToken);
    Task<int> UpdatePaymentsAdjustmentListsAsync(List<PaymentsAdjustmentsEntity> paymentsAdjustmentsEntity, CancellationToken cancellationToken);
    Task<int?> GetChargeEntryIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
    Task<int?> GetPaymentIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
    Task<ClaimChargeEntryEntity?> GetChargeByIdAsync(int chargeId, CancellationToken cancellationToken);
    Task<PaymentsAdjustmentsResponseModel> GetPaymentsAdjustmentsAsync(PaymentsAdjustmentsRequestModel model, CancellationToken cancellationToken);
    Task<ClaimFollowUpResponseModel> GetClaimFollowUpReportAsync(ClaimFollowUpRequestModel model, CancellationToken cancellationToken);
    Task<byte[]> ExportToExcelAsync(PaymentsAdjustmentsRequestModel model, PaymentsAdjustmentsResponseModel response, CancellationToken cancellationToken);
    Task<byte[]> ExportToExcelClaimFollowAsync(ClaimFollowUpRequestModel model, List<ClaimFollowUpResponse> claimFollowUpResponses, CancellationToken cancellationToken);
    
}
