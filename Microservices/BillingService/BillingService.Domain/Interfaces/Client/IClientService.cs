using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Models.Funders;
using Rethink.Services.Common.Models.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Client
{
    public interface IClientService
    {
        Task<List<ClientOptionModel>> GetClientsListForClaimAsync(int accountId, int memberId);

        Task<List<ClientDiagnosis>> SearchDiagnosis(string searchTerm, int? diagnosisTypeId, int accountInfoId, int? excludeDiagnosisTypeId = null);

        Task<List<BaseNameOption>> GetClientAuthorizationsForClaimAsync(int childProfileId, int funderId, int clientFunderServiceLineId, int accountInfoId);

        Task<List<Models.Clients.ClientFunderModel>> GetClientFundersAsync(int clientId, int accountInfoId, bool loadAllFunderTypes = false);
        Task<int> GetClientFacilityIdAsync(int clientId, int accountInfoId);
        Task<List<FunderServiceLineModel>> GetFunderServiceLinesAsync(int id, int funderId, int accountInfoId, int clientId);

        Task<ClientFunderResponsiblePartiesModel> GetClientFunderResponsiblePartiesAsync(int memberId, int accountId, int clientId, int clientFunderId);

        Task<ClientAuthorizationModel> GetClientAuthorization(int authorizationId, int childProfileId, int memberId,
            int accountInfoId, string LocaleString);

        Task<ClaimCreateInfoModel> GetClaimCreateInfoAsync(ClaimCreateInfoGetModel model, int accountInfoId);

        Task<List<DiagnosisCodeForClaimWithoutAuthModel>> GetDiagnosisForClaimWithoutAuthAsync(int clientId, int serviceLine, int accountInfoId);
    }
}
