using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class FunderService : BaseService, IFunderService
    {
        private readonly IMapper _mapper;
        private readonly IRethinkMasterDataMicroServices _rethinkMasterDataMicroServices;

        public FunderService(IMapper mapper,
            IRethinkMasterDataMicroServices rethinkMasterDataMicroServices)
        {
            _rethinkMasterDataMicroServices = rethinkMasterDataMicroServices;
            _mapper = mapper;
        }

        //public async Task<List<FunderItem>> GetForAccount(int accountInfoId)
        //{
        //    var funders = await _funderRepository.Query()
        //        .Where(x => x.AccountInfoId == accountInfoId && x.DateDeleted == null && x.IsActive == true)
        //        .ToListAsync();

        //    return _mapper.Map<List<FunderItem>>(funders);
        //}

        public async Task<FunderDropdownResponseModel> GetFundersAsync(FunderSearchModelWithUserInfo funderSearchModel)
        {
            var funder = await _rethinkMasterDataMicroServices.GetFunderList(funderSearchModel.AccountInfoId);

            var fundersBaseQuery = funder.data.AsQueryable().Where(x => x.FunderName.ToLower().Contains(funderSearchModel.FunderName.ToLower()));

            if (funderSearchModel.Take > 0)
            {
                fundersBaseQuery = fundersBaseQuery
                    .OrderBy(x => x.FunderName)
                    .Skip(funderSearchModel.Skip)
                    .Take(funderSearchModel.Take);
            }
            else
            {
                fundersBaseQuery = fundersBaseQuery.OrderBy(x => x.FunderName);
            }

            var funders = fundersBaseQuery
                .Select(f => new FunderDropdownModel
                {
                    Id = f.Id,
                    FunderName = f.FunderName
                })
                .ToList();

            var funderCount = funders.Count();

            var response = new FunderDropdownResponseModel
            {
                Funders = funders,
                TotalCount = funderCount
            };

            //var sql = funders.ToSql();

            return response;
        }
    }
}