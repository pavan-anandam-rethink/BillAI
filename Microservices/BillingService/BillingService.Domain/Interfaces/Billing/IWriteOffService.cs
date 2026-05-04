using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Claims.WriteOff;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IWriteOffService
    {
        Task<AddWriteOffResponseModel> AddAsync(WriteOffClaimModelWithUserInfo model);
        Task<List<ClaimChargeEntryWriteOffModel>> GetChargeEntryWriteOffsByChargeIdAsync(GetChargeEntryWriteOffModel model);
        Task<List<WriteOffReasonCodDescriptionModel>> GetReasonCodesAsync();
        Task DeleteChargeEntryWriteOffsByChargeIdAsync(IdsWithUserInfo model);
        Task<List<ClaimChargeEntryWriteOffModel>> UpdateChargeEntryWriteOffsByChargeIdAsync(EditChargeEntryWriteOffModelWithUserInfo model);
    }
}
