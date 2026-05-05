using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Domain.Interfaces;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services
{
    public class ClearingHouseService : IClearingHouseService
    {
        private readonly IMessageBus _bus;

        public ClearingHouseService(IMessageBus bus)
        {
            _bus = bus;
        }

        public async Task SubmitClaimAsync(ClaimSubmitModel model)
        {
            var submissionInfoModel = new ClaimSubmissionStart
            {
                AccountInfoId = model.AccountInfoId,
                MemberId = model.MemberId,
                ClaimId = model.Id,
                ClearingHouseId = model.ClearinghouseId.Value,
                PendingClaimSubmissionId = model.PendingClaimSubmissionId,
                AdjustmentLevel = model.AdjustmentLevel,
                IsSecondary = model.IsSecondary,
            };
            await _bus.SendAsync(submissionInfoModel, Queues.RT_Billing_ClearingHouse_ClaimSubmission);
        }
    }
}
