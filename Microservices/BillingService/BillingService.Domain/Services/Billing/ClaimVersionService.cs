using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimVersionService : BaseService, IClaimVersionService
    {
        private readonly IRepository<BillingDbContext, ClaimVersionEntity> _claimVersionRepository;

        public ClaimVersionService(IRepository<BillingDbContext, ClaimVersionEntity> claimVersionRepository)
        {
            _claimVersionRepository = claimVersionRepository;
        }

        public async Task<int> CreateAsync(ClaimDetailsModel claim, int accountInfoId, int memberId)
        {
            var claimVersionEntity = MapToHistoryVersion(claim);
            claimVersionEntity.ClaimId = claim.Id;
            claimVersionEntity.Identifier = Guid.NewGuid();
            claimVersionEntity.ClaimIdentifier = claim.ClaimIdentifier;
            claimVersionEntity.AccountInfoId = accountInfoId;
            claimVersionEntity.MemberId = memberId;
            claimVersionEntity.Status = claim.ClaimStatus;

            MarkCreated(claimVersionEntity, claimVersionEntity.MemberId);
            await _claimVersionRepository.AddAsync(claimVersionEntity);
            await _claimVersionRepository.SaveChangesAsync();

            return claimVersionEntity.Id;
        }

        public async Task<ClaimVersionEntity> GetByIdAsync(int id)
        {
            return await _claimVersionRepository.GetByIdAsync(id) ?? new ClaimVersionEntity();
        }

        private ClaimVersionEntity MapToHistoryVersion(ClaimDetailsModel claim)
        {
            var result = new ClaimVersionEntity();

            // Client Info
            result.ClientName = claim.PatientName;
            result.ResponsibleParty = claim.ResponsibleParty;
            result.StartDate = claim.DateOfServiceStart;
            result.EndDate = claim.DateOfServiceEnd;
            result.DiagnosisCodes = string.Join(", ", claim.DiagnosisCodes
                .Select(x => x.DiagnosisCode)
                .ToList());
            result.AuthorizationNumber = claim.AuthorizationNumber;

            // Charge Detail Summary
            result.BalanceAmount = claim.BalanceAmount;
            result.PaymentAmount = claim.PaymentAmount;
            result.BilledAmount = claim.BilledAmount;
            result.PatientResponsibilityAmount = claim.PatientResponsibilityAmount;
            result.PlaceOfService = claim.PlaceOfService;

            // Providers
            result.ReferringProvider = claim.ReferringProvider;
            result.BillingProvider = claim.BillingProvider;
            result.ReferringProvider = claim.ReferringProvider;
            result.ServiceProvider = claim.ServiceFacility;

            // Additional Info
            result.SubmissionReason = claim.SubmissionReason;
            result.AuthorizedReleaseOfInfo = GetConfirmationType(claim.PatientReleaseAgreement);
            result.AuthorizePayment = GetConfirmationType(claim.AuthorizePayment);
            result.SubmissionCode = claim.SubmissionCode;
            result.OriginalClaim = claim.OriginalClaim;
            result.Note = claim.Note;

            return result;
        }

        private string GetConfirmationType(int? typeId)
        {
            switch (typeId)
            {
                case 1: return "Yes";
                case 2: return "No";
                case 3: return "Not applicable";
                default: return null;
            }
        }
    }
}
