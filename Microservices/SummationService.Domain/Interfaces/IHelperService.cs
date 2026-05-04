using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.ReportingModels;

namespace SummationService.Domain.Interfaces;

public interface IHelperService
{
    Task<int?> GetClaimIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken);

    Task<int?> GetClaimIdFromPaymentIdAsync(int transactionTypeId, CancellationToken cancellationToken);

    Task<int?> GetClaimIdFromWriteOffIdAsync(int transactionTypeId, CancellationToken cancellationToken);

    Task<int?> GetClaimIdFromChargeEntryIdAsync(int transactionTypeId, CancellationToken cancellationToken);

    Task<List<Tuple<bool?, decimal?>>> GetAdjustmentsFromClaimIdAsync(int claimId, ClaimTransactionType adjustmentTransactionType);
    Task<string> GetLocationName(int locationId);
    Task<decimal> CalculateClaimPaymentSumAsync(int claimId, int paymentTypeId);

    Task<decimal> CalculateClaimWriteOffSumAsync(int claimId);

    Task<decimal> GetBilledAmountByClaimIdAsync(int claimId);

    Task<List<ClaimChargeEntryEntity>> GetChargeEntriesByClaimId(int claimId);
    Task<int?> GetChargeIdFromWriteOffIdAsync(int transactionTypeId, CancellationToken cancellationToken);
    Task<int?> GetChargeIdFromPaymentIdAsync(int transactionTypeId, CancellationToken cancellationToken);
    Task<int?> GetChargeIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken);
    Task<int?> GetPaymentIdFromWriteOffIdAsync(int transactionTypeId, CancellationToken cancellationToken);
    Task<int?> GetPaymentIdFromPaymentIdAsync(int transactionTypeId, CancellationToken cancellationToken);
    Task<int?> GetPaymentIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken);
    Task<List<AccountsReceivableQueryModel>> GetAccountsReceivableEntitiesByFunderIdAsync(List<int> funderIds, DateTime closingDate, int accountInfoId, CancellationToken cancellationToken);
    Task<List<PaymentsAdjustmentsResponse>> GetPaymentsAdjustmentsByFunderIdAndDateAsync(List<int>? funderIds, DateTime startDate, DateTime endDate, ReportingDateRangeType rangeType, int accountInfoId, CancellationToken cancellationToken);
    Task<(List<ClaimFollowUpResponse> Data, int Total)> GetClaimFollowUpReportData(ClaimFollowUpRequestModel model, CancellationToken cancellationToken);
    
    void DefineStyles(WorkbookPart workbookPart);
    Cell AddCell(ExcelCellType type, dynamic value, bool isAlternateRowColor = false);
    Task<string> GetFunderName(int funderId);
    Task<int?> GetChargeEntryIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken);
}
