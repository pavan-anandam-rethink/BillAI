using BillingService.Domain.Interfaces.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;
using System.Linq;
using System.Threading.Tasks;
namespace BillingService.Domain.Services.Billing
{
    public class Eligibility271ResponseService : BaseService, IEligibility271ResponseService
    {
        private readonly IRepository<BillingDbContext, Eligibility271ResponseEntity> _repository;
        public Eligibility271ResponseService(IRepository<BillingDbContext, Eligibility271ResponseEntity> repository)
        {
            _repository = repository;
        }

        public Task<EligibilityResponse> GetEligibilityResponse(EligibilityRequest request)
        {
            var entity = _repository.GetMany(e => e.FunderId == request.FunderId 
                && e.CreatedBy == request.CreatedBy
                && e.CreatedDate.HasValue
                && e.CreatedDate.Value.Date == request.CreatedDate.Date)
                .FirstOrDefault();

            if (entity == null)
            {
                return null;
            }

            var response = new EligibilityResponse
            {
                FunderId = entity.FunderId,
                AccountId=entity.AccountId,
                EffectiveStartDate = entity.EffectiveStartDate,
                EffectiveEndDate = entity.EffectiveEndDate,
                CoverageStatus = entity.CoverageStatus,
                SubscriberStartDate = entity.SubscriberStartDate,
                SubscriberEndDate = entity.SubscriberEndDate,
                PlanStartDate = entity.PlanStartDate,
                PlanEndDate = entity.PlanEndDate,
                FailureResponse = entity.FailureResponse ?? string.Empty,
            };
            return Task.FromResult(response);
        }
    }
}