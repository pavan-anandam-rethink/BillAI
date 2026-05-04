using BillingService.Domain.DataObjects.CompanyAccount;
using BillingService.Domain.Interfaces.Common;
using Newtonsoft.Json;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Common
{
    public class CommonService : BaseService, ICommonService
    {
        private readonly IRethinkMasterDataMicroServices _rethink;
        public const string revSpringOrgSiteId = "revSpringOrgSiteId";

        public CommonService(IRethinkMasterDataMicroServices rethink)
        {
            _rethink = rethink;
        }

        JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        public async Task<List<LocationCodeData>> GetLocationCodes(int accountInfoId)
        {
            var locationCodes = new List<LocationCodeData>();
            var existingPlaceOfServices = await _rethink.GetPlaceOfService(accountInfoId);

            if (existingPlaceOfServices != null && existingPlaceOfServices.placesOfService.data.Any())
            {
                var locationCode = await _rethink.GetLocationCodes();
                locationCodes = locationCode.Select(c => new LocationCodeData
                {
                    Id = c.id,
                    Description = c.description,
                    Code = c.code,
                }).ToList();
                foreach (var item in locationCodes)
                {
                    var matchedPos = existingPlaceOfServices.placesOfService.data.Find(epos => epos.code == item.Code);
                    if (matchedPos != null)
                    {
                        item.Description = matchedPos.description;
                        item.IsActive = matchedPos.isActive;
                    }
                }
            }
            return locationCodes;
        }
    }
}