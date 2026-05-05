using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.ReportingModels;

namespace SummationService.Domain.Interfaces;

public interface IAccountsReceivableService
{
    Task<int?> FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);

    Task<ClaimEntity?> GetClaimByIdAsync(int claimId, CancellationToken cancellationToken);

    Task<AccountsReceivableEntity?> GetAccountsReceivableByIdAsync(int claimId, CancellationToken cancellationToken);

    Task<AccountsReceivableEntity?> PrepareAccountsReceivableAsync(ClaimTransactionType transactionType, ClaimEntity claim, CancellationToken cancellationToken);

    Task AddAccountsReceivableAsync(AccountsReceivableEntity accountsReceivableEntity, CancellationToken cancellationToken);

    //Update includes soft-delete
    void UpdateAccountsReceivable(AccountsReceivableEntity accountsReceivableEntity, CancellationToken cancellationToken);
    Task<bool> AddOrUpdateAccountsReceivableAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
    Task<AccountsReceivablesResponseModel> GetAccountsReceivablesAsync(AccountsRecievablesRequestModel model, CancellationToken cancellationToken);
    Task<AccountsReceivablesChargeLevelResponseModel> GetAccountsReceivablesChargeLevelAsync(AccountsRecievablesRequestModel model, CancellationToken cancellationToken);
    Task<List<FunderDetailsResponseModel>> GetFundersAsync(CancellationToken cancellationToken);
    Task<byte[]> ExportToExcelAsync(AccountsRecievablesRequestModel model, AccountsReceivablesResponseModel response, CancellationToken cancellationToken);
    Task<byte[]> ExportToExcelChargeLevelAsync(AccountsRecievablesRequestModel model, AccountsReceivablesChargeLevelResponseModel response, CancellationToken cancellationToken);
}
