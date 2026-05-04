using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Models;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IChargeEntryService
    {
        Task<ClaimChargeEntryEntity> GetChargeEntityWithChargePaymentsAsync(int chargeEntryId, int claimId);
        Task<List<ClaimChargeEntryEntity>> GetChargeEntitiesWithChargePaymentsAsync(int claimId);
        Task<List<ClaimChargeEntryEntity>> GetChargeEntitiesWithChargePaymentsAsync(IEnumerable<int> claimIds);
        Task AddChargePaymentAsync(ChargePaymentEntity entity, bool commitImmediately = true);
        Task UpdateChargeEntryAsync(ClaimChargeEntryEntity entity, bool commitImmediately = true);
        Task UpdateChargePaymentAsync(ChargePaymentEntity entity, bool commitImmediately = true);
        Task<int> GetMaxChargePaymentIdAsync();

        Task<ChargeNoteModel> AddChargeNoteAsync(AddNoteModel model);
        Task DeleteChargeNoteAsync(int chargeId);
        Task<List<ClaimChargeItem>> GetIdsAllOpenedPatientClaimAsync(int patientId);
        Task<List<ClaimChargeItem>> GetAllClaimsByIdAsync(PaymentEntity payment, int[] claimIds);
    }
}
