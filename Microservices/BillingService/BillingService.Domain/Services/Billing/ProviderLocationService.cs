using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
   public class ProviderLocationService : BaseService, IProviderLocationService
    {

        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IMapper _mapper;

        public ProviderLocationService(
            IRethinkMasterDataMicroServices rethinkServices,
            IMapper mapper
            )
        {
            _rethinkServices = rethinkServices;
            _mapper = mapper;
        }

        public async Task<List<ProviderLocations>> GetForAccount(int accountInfoId)
        {
            var providerLocations = await _rethinkServices.GetProviderLocationList(accountInfoId);

            var data = providerLocations.data.Where(x => x.metaData.deletedOn == null).ToList();

            return data;
        }
    }
}
