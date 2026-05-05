using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.PatientInvoice
{
    public class ClientInfoService : BaseService, IClientInfoService
    {
        private readonly IRethinkMasterDataMicroServices _rethinkMasterDataMicroServices;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private List<StateModel> _states;
        public ClientInfoService(IRethinkMasterDataMicroServices rethinkMasterDataMicroServices,
            IRepository<BillingDbContext, ClaimEntity> claimRepository)
        {
            _rethinkMasterDataMicroServices = rethinkMasterDataMicroServices;
            _claimRepository = claimRepository;
        }

        public async Task<ClientInfo> GetClientInfo(int accountId, int clientId)
        {
            var clientDetails = await _rethinkMasterDataMicroServices.GetChildProfileReturningEntity(accountId, clientId);
            if (clientDetails == null)
            {
                throw new Exception("Client details not found");
            }
            return new ClientInfo
            {
                CustomerID = clientDetails.Id.ToString(),
                Name = $"{clientDetails.FirstName} {clientDetails.MiddleName ?? ""} {clientDetails.LastName}",
                Address = clientDetails.Address ?? "",
                City = clientDetails.City ?? "",
                Town = clientDetails.Town ?? "",
                State = clientDetails?.StateLU?.name ?? "",
                ZipCode = clientDetails?.ZipCode ?? "",
                Country = clientDetails?.CountryLU?.name ?? ""
            };
        }
        public async Task<BillingProviderInfo> GetBillingProviderInfo(int accountId, int clientId)
        {
            var providerLocationId = _claimRepository.Query()
                .Where(x => x.AccountInfoId == accountId && x.ChildProfileId == clientId && x.DateDeleted == null)
                .OrderByDescending(x => x.DateLastModified)
                .Select(x => x.ProviderLocationId)
                .FirstOrDefault();

            var providerLocation = providerLocationId.HasValue
                ? await _rethinkMasterDataMicroServices.GetProviderLocation(accountId, providerLocationId.Value)
                : null;

            return providerLocation != null
                ? new BillingProviderInfo
                {
                    Name = providerLocation.agencyName ?? "N/A",
                    Address = $"{providerLocation.address.street1}\n{providerLocation.address.street2 ?? ""}\n{providerLocation.address.city}, {await GetState(providerLocation.address.stateId)} {providerLocation.address.zip}",
                    Phone = providerLocation.phone ?? "N/A"
                }
                : new BillingProviderInfo();
        }

        private async Task<string> GetState(int? stateId)
        {
            if (_states == null)
            {
                _states = await _rethinkMasterDataMicroServices.GetStateList();
            }
            var state = _states.FirstOrDefault(s => s.id == stateId);
            return state?.abbreviation;
        }

    }
}
