using AutoMapper;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using Rethink.Services.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class LocationCodeService : BaseService, ILocationCodeService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;

        public LocationCodeService(ICommonService commonService, IMapper mapper)
        {
            _commonService = commonService;
            _mapper = mapper;
        }

        public async Task<List<LocationCodeItem>> GetAll(int accountInfoId)
        {
            var locationCodes = await _commonService.GetLocationCodes(accountInfoId);
            return locationCodes.Select(lc => new LocationCodeItem()
            {
                Id = lc.Id,
                Code = lc.Code,
                Description = lc.Description
            }).ToList();
        }
    }
}
