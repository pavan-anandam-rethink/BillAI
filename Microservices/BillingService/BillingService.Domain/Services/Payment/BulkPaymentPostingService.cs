using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.BulkPaymentPosting;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Payment
{
    public class BulkPaymentPostingService : BaseService, IBulkPaymentPostingService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _claimChargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> _claimChargeEntryWriteOffRepository;

        public BulkPaymentPostingService(
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IMapper mapper,
            IPaymentClaimService paymentClaimService,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
            IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository)
        {
            _paymentClaimRepository = paymentClaimRepository;
            _mapper = mapper;
            _paymentClaimService = paymentClaimService;
            _claimChargeEntryRepository = claimChargeEntryRepository;
            _claimChargeEntryWriteOffRepository = claimChargeEntryWriteOffRepository;
        }

        public async Task<List<BulkPaymentResponseModel>> GetAllPayments(BulkPaymentPostingRequestModel paymentForPosting)
        {
            // 1. Load payment claims (read-only)
            var paymentClaims = await _paymentClaimRepository.Query()
                .AsNoTracking()
                .Where(x => paymentForPosting.Ids.Contains(x.Id))
                .Include(x => x.Payment)
                .ToListAsync();

            if (!paymentClaims.Any())
                return new List<BulkPaymentResponseModel>();

            var result = new List<BulkPaymentResponseModel>();
            var paymentClaim = paymentClaims.FirstOrDefault();

            var allChargeData = await _paymentClaimService.GetAllCharges(paymentClaim.Payment.Id);
            var groupedData = await _paymentClaimService.GetGroupedByPayments(allChargeData, paymentClaim.Payment, GroupByParam.Charge, true);

            // 🔹 Lookups for fast access
            var chargeLookup = allChargeData
                .Where(x => x.DateDeleted == null && x.PaymentId == paymentClaim.PaymentId)
                .GroupBy(x => (x.PaymentClaimId, x.PaymentId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var groupedLookup = groupedData.Where(x => x.ChargeId != 0).ToDictionary(x => x.ChargeId);

            // 🔹 Collect all ChargeEntryIds once
            var chargeEntryIds = allChargeData.Where(x => x.ChargeId != 0).Select(x => x.ChargeId).Distinct().ToList();

            // 🔹 Preload write-offs
            var writeOffLookup = await _claimChargeEntryWriteOffRepository.Query()
                .AsNoTracking()
                .Where(x => chargeEntryIds.Contains(x.ClaimChargeEntryId)
                            && x.DateDeleted == null)
                .GroupBy(x => x.ClaimChargeEntryId)
                .Select(g => new
                {
                    ChargeId = g.Key,
                    Amount = g.Sum(x => x.WriteOffAmount) ?? 0
                })
                .ToDictionaryAsync(x => x.ChargeId, x => x.Amount);

            // 🔹 Preload modifiers
            var modifierLookup = await _claimChargeEntryRepository.Query()
                .AsNoTracking()
                .Where(x => chargeEntryIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Modifier1,
                    x.Modifier2,
                    x.Modifier3,
                    x.Modifier4
                })
                .ToDictionaryAsync(x => x.Id);

            // 🔹 Total count once
            var totalCount = await _paymentClaimRepository.Query().CountAsync();

            foreach (var pc in paymentClaims)
            {
                if (!chargeLookup.TryGetValue((pc.Id, pc.PaymentId), out var filteredData))
                    continue;

                var paymentResponses = filteredData.Select(x => new BulkPaymentResponseModel
                {
                    Id = x.ServiceLineId,
                    ServiceLineId = x.ServiceLineId,
                    ClaimId = pc.ClaimId,
                    ClaimIdentifier = pc.ClaimIdentifier,
                    MemberId = paymentForPosting.MemberId,
                    AccountInfoId = paymentForPosting.AccountInfoId,
                    PatientName = pc.ClientFirstName + " " +
                                  (pc.ClientMiddleName != null ? pc.ClientMiddleName + " " : "") +
                                  pc.ClientLastName,
                    PatientId = x.PatientId,
                    ChargeEntryId = x.ChargeId,
                    DateOfService = (DateTime)x.DateOfService,
                    Procedure = x.ServiceCode,
                    AllowedAmount = x.AllowedAmount ?? 0,
                    BilledAmount = x.ChargeAmount ?? 0,
                    PaidAmount = x.PaidAmount ?? 0,
                    Mods = string.Join(",",
                        x.ProcedureModifier1,
                        x.ProcedureModifier2,
                        x.ProcedureModifier3,
                        x.ProcedureModifier4),
                    DateLastModified = x.DateLastModified,
                    DateDeleted = x.DateDeleted,
                    PatientResponsibility = 0,
                    PatientResponsibilityBalance = 0,
                    Adjustments = _mapper.Map<List<PaymentClaimServiceLineAdjustmentModel>>(x.Adjustments),
                    Status = pc.ClaimStatus == "22"
                        ? "Reversal"
                        : pc.ClaimStatus == ((int)ClaimStatus.Pending).ToString()
                            ? ClaimStatus.Pending.ToString()
                            : "Processed",
                    Balance = 0,
                    HasErrors = x.HasErrors
                }).ToList();

                result.AddRange(paymentResponses);

                foreach (var pr in paymentResponses)
                {
                    if (!pr.ChargeEntryId.HasValue ||
                        !groupedLookup.TryGetValue(pr.ChargeEntryId.Value, out var paymentInfo))
                        continue;

                    pr.PatientResponsibility = paymentInfo.PatientResponsibility;
                    pr.PatientResponsibilityBalance =
                        paymentInfo.PatientResponsibilityBalance ?? 0;

                    pr.Adjustment = paymentInfo.Adjustment;

                    //Write-off lookup
                    var writeOff = writeOffLookup.GetValueOrDefault(pr.ChargeEntryId.Value);
                    pr.WriteOff = writeOff;
                    pr.Adjustment += writeOff;

                    pr.InsurancePayment = paymentInfo.InsurancePayment;
                    pr.PatientPayment = paymentInfo.PatientPayment;

                    //Modifier lookup
                    if (modifierLookup.TryGetValue(pr.ChargeEntryId.Value, out var m))
                    {
                        pr.Mods = string.Join(", ",
                            new[] { m.Modifier1, m.Modifier2, m.Modifier3, m.Modifier4 }
                            .Where(x => !string.IsNullOrWhiteSpace(x)));
                    }
                    else
                    {
                        pr.Mods = string.Empty;
                    }

                    pr.Balance = pr.BilledAmount -
                                 (pr.InsurancePayment +
                                  pr.Adjustment +
                                  pr.PatientResponsibility);

                    pr.ExpectedAmount = pr.BilledAmount;

                    //Count only once
                    pr.TotalCount = totalCount;
                }
            }

            return result
                .OrderByDescending(x => x.PatientId)
                .ThenByDescending(x => x.DateOfService)
                .ToList();

        }

    }
}