using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ProviderBillingCodeService : BaseService, IProviderBillingCodeService
    {
        private readonly IMapper _mapper;
        private readonly IRethinkMasterDataMicroServices _rethinkMasterDataService;

        public ProviderBillingCodeService(IMapper mapper, IRethinkMasterDataMicroServices rethinkMasterDataService)
        {
            _rethinkMasterDataService = rethinkMasterDataService;
            _mapper = mapper;
        }

        //public async Task<List<ProviderBillingCodeItem>> GetForFunders(int accountInfoId, List<int> funderIds)
        //{
        //    var providerBillingCodes = await _providerBillingCodeRepository.Query()
        //        .Include(bc => bc.ProviderService)
        //        .Where(x => x.AccountInfoId == accountInfoId && funderIds.Contains(x.FunderId) && x.DateDeleted == null)
        //        .ToListAsync();

        //    return _mapper.Map<List<ProviderBillingCodeItem>>(providerBillingCodes);
        //}

        public async Task<decimal?> GetServiceRateAsync(int funderId, string serviceCode, int accountInfoId)
        {
            var billingCodes = await _rethinkMasterDataService.GetProviderBillingCodeList(accountInfoId);
            var billingCode = billingCodes
                .FirstOrDefault(bc => bc.funderId == funderId && bc.billingCode == serviceCode);

            return billingCode?.rate;
        }
    }
}