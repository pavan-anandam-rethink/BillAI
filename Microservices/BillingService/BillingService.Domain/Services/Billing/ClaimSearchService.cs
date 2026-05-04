using AutoMapper;
using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Interfaces.Billing;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver.Linq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimSearchService : BaseService, IClaimSearchService
    {
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchClientEntity> _clientSearchRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchFunderEntity> _funderSearchRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity> _staffSearchRepository;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IMapper _mapper;

        public ClaimSearchService(
            IRepository<BillingDbContext, ClaimEntity> clientRepository,
            IRepository<BillingDbContext, ClaimSearchClientEntity> clientSearchRepository,
            IRepository<BillingDbContext, ClaimSearchFunderEntity> funderSearchRepository,
            IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity> staffSearchRepository,
            IRethinkMasterDataMicroServices rethinkServices,
            IMapper mapper
            )
        {
            _claimRepository = clientRepository;
            _clientSearchRepository = clientSearchRepository;
            _funderSearchRepository = funderSearchRepository;
            _staffSearchRepository = staffSearchRepository;
            _rethinkServices = rethinkServices;
            _mapper = mapper;
        }

        public async Task<List<BaseNameOption>> GetAllClientsForAccount(int accountInfoId)
        {
            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(accountInfoId);
            var clientList = _mapper.Map<List<BaseNameOption>>(clientUsersList);
            return clientList;
        }

        public async Task<List<BaseNameOption>> GetFunderInfoByIds(int accountInfoId)
        {
            var unbilledClaims = await GetUnbilledClaims(accountInfoId);
            var funderIds = unbilledClaims.Select(x => x.PrimaryFunderId).ToList();

            var result = await _funderSearchRepository
                                .Query()
                                .Where(c => funderIds.Contains(c.Id))
                                .Select(c => new BaseNameOption { Id = c.Id, Name = c.Name })
                                .ToListAsync();
            return result;
        }

        public async Task<List<BaseNameOption>> GetClientHistoryFunderInfoByIds(int accountInfoId, int clientId)
        {
            var claims = await _claimRepository
                                .Query()
                                .Where(x => x.AccountInfoId == accountInfoId
                                            && x.ChildProfileId == clientId
                                            && x.ClaimAppointmentLinks.Any())
                                .ToListAsync();
            var funderIds = claims.Select(x => x.PrimaryFunderId).Distinct().ToList();

            var result = await _funderSearchRepository
                                .Query()
                                .Where(c => funderIds.Contains(c.Id))
                                .Select(c => new BaseNameOption { Id = c.Id, Name = c.Name })
                                .ToListAsync();
            return result;
        }

        public async Task<List<StaffBaseNameOption>> GetStaffInfoByIds(int accountInfoId)
        {
            List<StaffBaseNameOption> staffBaseNameOption = new List<StaffBaseNameOption>();
            var unbilledClaims = await GetUnbilledClaims(accountInfoId);
            var staffIds = unbilledClaims
                .Where(x => x.RenderingStaffMemberId != null)
                .Select(x => new { typeId = x.RenderingProviderTypeId, staffId = x.RenderingStaffMemberId })
                .Distinct()
                .ToList();

            var staffIdList = staffIds
                .Where(s => s.staffId.HasValue)
                .Select(s => s.staffId!.Value)
                .ToList();
            var staffsDict = await _staffSearchRepository.Query()
                .Where(x => staffIdList.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            staffIds.ForEach(staff =>
            {
                string name = null;
                if (staff.staffId.HasValue)
                {
                    staffsDict.TryGetValue(staff.staffId.Value, out name);
                }
                staffBaseNameOption.Add(new StaffBaseNameOption { Id = staff.staffId, Name = name, TypeId = staff.typeId });
            });

            return staffBaseNameOption;
        }

        public async Task<List<BaseNameOption>> GetPlaceOfServiceInfoByIds(int accountInfoId)
        {
            var locationCodes = await _rethinkServices.GetLocationCodes();

            if(locationCodes == null || !locationCodes.Any())
            {
                return new List<BaseNameOption>();
            }

            var result = locationCodes.Select(x => new BaseNameOption
            {
                Id = x.id,
                Name = $"{x.code} - {x.description}"
            }).ToList();

            return result;
        }

        public async Task<List<BaseNameOption>> GetLocationInfoByIds(int accountInfoId)
        {
            var locationCodes = await _rethinkServices.GetProviderLocationList(accountInfoId);

            if (locationCodes == null || locationCodes.data == null || !locationCodes.data.Any())
            {
                return new List<BaseNameOption>();
            }
            var result = locationCodes.data.Select(x => new BaseNameOption
            {
                Id = x.id,
                Name = x.name
            }).ToList();

            return result;
        }

        private async Task<List<ClaimEntity>> GetUnbilledClaims(int accountInfoId)
        {
            return await _claimRepository
                                .Query()
                                .Where(x => x.AccountInfoId == accountInfoId
                                            && (x.ClaimStatus == ClaimStatus.PendingReview || x.ClaimStatus == ClaimStatus.ReadyToBill)
                                            && x.ClaimAppointmentLinks.Any())
                                .ToListAsync();
        }
    }

    public class StaffBaseNameOption
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int? TypeId { get; set; }
    }
}
