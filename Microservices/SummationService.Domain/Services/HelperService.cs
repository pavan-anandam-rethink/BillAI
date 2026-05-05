using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using SummationService.Domain.Interfaces;
using System.Linq;

namespace SummationService.Domain.Services;

public class HelperService(IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustmentRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentclaimServiceLineRepository,
    IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository,
    IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
    IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
    IRepository<ReportingDbContext, AccountsReceivableEntity> accountsReceivableRepository,
    IRepository<ReportingDbContext, ClaimStatusEntity> claimStatusReportingRepository,
    IRepository<ReportingDbContext, ClientsEntity> clientNameReportingRepository,
    IRepository<ReportingDbContext, FundersEntity> funderNameReportingRepository,
    IRepository<ReportingDbContext, PaymentsAdjustmentsEntity> paymentAdjustmentsRepository,
    IRepository<BillingDbContext, ClaimNoteEntity> claimNoteRepository,
    IRepository<BillingDbContext, PaymentEntity> paymentRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
    IRepository<BillingDbContext, ClaimVersionEntity> claimVersionRepository,
    IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity> claimSearchRenderingProvidersRepository,
    IRepository<BillingDbContext, ClaimSearchLocationEntity> claimSearchLocationRepository,
    IRepository<BillingDbContext, ClaimEntity> claimRepository
    ) : BaseService, IHelperService
{
    public async Task<int?> GetClaimIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await paymentClaimServiceLineAdjustmentRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaimServiceLine.PaymentClaim.ClaimId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string> GetLocationName(int locationId)
    {
        var locationName = await claimSearchLocationRepository.Query()
    .Where(x => x.Id == locationId && x.DateDeleted == null)
    .Select(x => x.Name)
    .FirstOrDefaultAsync();

        return locationName ?? string.Empty;
    }

    public async Task<int?> GetChargeEntryIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await paymentClaimServiceLineAdjustmentRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaimServiceLine.ClaimChargeEntryId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> GetPaymentIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await paymentClaimServiceLineAdjustmentRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaimServiceLine.PaymentClaim.PaymentId).FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<int?> GetChargeIdFromAdjustmentIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await paymentClaimServiceLineAdjustmentRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaimServiceLine.ClaimChargeEntryId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> GetClaimIdFromPaymentIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await paymentclaimServiceLineRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaim.ClaimId).FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<int?> GetChargeIdFromPaymentIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await paymentclaimServiceLineRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimChargeEntryId).FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<int?> GetPaymentIdFromPaymentIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await paymentclaimServiceLineRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaim.PaymentId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> GetClaimIdFromWriteOffIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await claimChargeEntryWriteOffRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimChargeEntry.ClaimId).FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<int?> GetChargeIdFromWriteOffIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await claimChargeEntryWriteOffRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimChargeEntry.Id).FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<int?> GetPaymentIdFromWriteOffIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        int chargeEntryId = await claimChargeEntryWriteOffRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimChargeEntry.Id).FirstOrDefaultAsync(cancellationToken);
        return await paymentclaimServiceLineRepository.Query().Where(x => x.ClaimChargeEntryId == chargeEntryId).Select(x => x.PaymentClaim.PaymentId).FirstOrDefaultAsync(cancellationToken);

    }

    public async Task<int?> GetClaimIdFromChargeEntryIdAsync(int transactionTypeId, CancellationToken cancellationToken)
    {
        return await claimChargeEntryRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> GetBilledAmountByClaimIdAsync(int claimId)
    {
        return await claimChargeEntryRepository.Query()
                                .Where(x => x.ClaimId == claimId && x.DateDeleted == null)
                                .Select(x => x.Charges)
                                .SumAsync();
    }
    public async Task<List<Tuple<bool?, decimal?>>> GetAdjustmentsFromClaimIdAsync(int claimId, ClaimTransactionType adjustmentTransactionType)
    {
        var adjustmentAmountList = await paymentClaimRepository.Query()
                                        .Where(x => x.ClaimId == claimId && x.DateDeleted == null)
                                        .SelectMany(pcs => pcs.PaymentClaimServiceLines
                                        .SelectMany(pcsa => pcsa.PaymentClaimServiceLineAdjustments
                                        .Where(pcsa => (IsAdjustmentTypePR(adjustmentTransactionType) ? (pcsa.AdjustmentGroupCode == prAdjustmentGroupCode) : (pcsa.AdjustmentGroupCode != prAdjustmentGroupCode)) && pcsa.DateDeleted == null)
                                        .Select(pcsa => new { pcsa.AdjustmentAmount, pcsa.IsAdjustmentPositive })
                                        )).ToListAsync();

        List<Tuple<bool?, decimal?>> adjustments = new List<Tuple<bool?, decimal?>>();
        foreach (var adjustment in adjustmentAmountList)
        {
            adjustments.Add(System.Tuple.Create(adjustment.IsAdjustmentPositive, adjustment.AdjustmentAmount));

        }

        return adjustments;
    }
    public async Task<decimal> CalculateClaimPaymentSumAsync(int claimId, int paymentTypeId)
    {
        var payment = await paymentClaimRepository.Query()
                .Where(x => x.ClaimId == claimId && x.DateDeleted == null
                && ((paymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.InsurancePayment
                || paymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.ERAReceived)
                ? (x.Payment.PaymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.InsurancePayment
                || x.Payment.PaymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.ERAReceived)
                : x.Payment.PaymentTypeId == paymentTypeId))
                .Select(x => x.PaymentClaimServiceLines
                .Where(pcs => pcs.DateDeleted == null)
                .Sum(pcs => pcs.PaymentAmount)).ToListAsync();

        var paymentSum = payment.Sum();
        return paymentSum ?? 0;
    }

    public async Task<decimal> CalculateClaimWriteOffSumAsync(int claimId)
    {
        return (await claimChargeEntryRepository.Query()
                                .Where(x => x.ClaimId == claimId && x.DateDeleted == null)
                                .Select(x => x.ClaimChargeEntryWriteOffs
                                            .Where(x => x.DateDeleted == null)
                                            .Select(x => x.WriteOffAmount)
                                            .Sum())
                                .ToListAsync()).Sum() ?? 0;
    }

    public async Task<List<ClaimChargeEntryEntity>> GetChargeEntriesByClaimId(int claimId)
    {
        return await claimChargeEntryRepository.Query().Where(x => x.ClaimId == claimId && x.DateDeleted == null).ToListAsync();
    }
    public async Task<List<AccountsReceivableQueryModel>> GetAccountsReceivableEntitiesByFunderIdAsync(List<int> funderIds, DateTime closingDate, int accountInfoId, CancellationToken cancellationToken)
    {
        funderIds ??= [];

        var allFunders = await funderNameReportingRepository.Query()
            .GroupBy(f => f.FunderId)
            .Select(g => g.First())
            .ToListAsync(cancellationToken);

        if(funderIds.Any())
        {
            allFunders = allFunders.Where(f => funderIds.Contains(f.FunderId)).ToList();
        }

        var accountReceivables = await accountsReceivableRepository.Query()
            .Where(accountsReceivables => !funderIds.Any() || funderIds.Contains(accountsReceivables.FunderId)
            && accountsReceivables.AccountInfoId == accountInfoId
            && accountsReceivables.BilledDate.Value.Date <= closingDate.Date
            && accountsReceivables.DateDeleted == null && accountsReceivables.DateCreated.Date <= closingDate.Date)
            .Join(claimStatusReportingRepository.Query(),
            accountsReceivables => accountsReceivables.ClaimStatusId,
            claimStatus => claimStatus.claimStatusId,
            (accountsReceivables, claimStatus) => new
            {
                accountsReceivables,
                claimStatus
            })
            .Select(combined => new
            {
                funderId = combined.accountsReceivables.FunderId,
                clientId = combined.accountsReceivables.ClientId,
                clientFirstName = string.Empty,
                clientLastName = string.Empty,
                claimFrom = combined.accountsReceivables.ClaimFrom,
                claimThrough = combined.accountsReceivables.ClaimThrough,
                claimStatus = combined.claimStatus.claimStatus,
                billedDate = combined.accountsReceivables.BilledDate,
                billedAmount = combined.accountsReceivables.BilledAmount,
                adjustments = combined.accountsReceivables.Adjustment,
                writeOff = combined.accountsReceivables.WriteOff,
                patientResponsibility = combined.accountsReceivables.PatientResponsibility,
                adjustedClaimAmount = combined.accountsReceivables.AdjustedClaimAmount,
                paymentReceived = combined.accountsReceivables.PaymentRecieved,
                netReceivable = combined.accountsReceivables.NetRecievable,
                dateDeleted = combined.accountsReceivables.DateDeleted,
                dateCreated = combined.accountsReceivables.DateCreated,
                dateModified = combined.accountsReceivables.DateModified,
                claimId = combined.accountsReceivables.ClaimId
            }).GroupBy(x => x.claimId)
            .Select(g => g.OrderByDescending(x => x.dateCreated).First()).ToListAsync(cancellationToken);

        var claimIds = accountReceivables.Select(x => x.claimId).ToList();

        var validClaimIds = claimChargeEntryRepository.Query()
            .Where(x => x.DateDeleted == null && claimIds.Contains(x.ClaimId))
            .Select(x => x.ClaimId)
            .Distinct()
            .ToHashSet();

        accountReceivables = accountReceivables.Where(ar => validClaimIds.Contains(ar.claimId)).ToList();

        var funderNameMap = allFunders.ToDictionary(f => f.FunderId, f => f.FunderName ?? string.Empty);

        var result = new List<AccountsReceivableQueryModel>();
        result.AddRange(accountReceivables.Where(x => x.dateDeleted == null && x.netReceivable != 0).Select(item => new AccountsReceivableQueryModel
        {
            FunderName = funderNameMap.ContainsKey(item.funderId) ? funderNameMap[item.funderId] : string.Empty,
            ClientId = item.clientId,
            ClaimId = item.claimId,
            ClientFirstName = item.clientFirstName,
            ClientLastName = item.clientLastName,
            ClaimFrom = item.claimFrom,
            ClaimThrough = item.claimThrough,
            ClaimStatus = item.claimStatus,
            BilledDate = item.billedDate,
            BilledAmount = item.billedAmount,
            Adjustments = item.adjustments,
            WriteOff = item.writeOff,
            PatientResponsibility = item.patientResponsibility,
            AdjustedClaimAmount = item.adjustedClaimAmount,
            PaymentReceived = item.paymentReceived,
            NetReceivable = item.netReceivable,
            DateCreated = item.dateCreated,
            DateModified = item.dateModified
        }));
        return result;
    }


    public async Task<string> GetFunderName(int funderId)
    {
        var funder = await funderNameReportingRepository.Query().Where(x => x.FunderId == funderId).FirstOrDefaultAsync();
        return funder.FunderName;
    }

    public async Task<List<PaymentsAdjustmentsResponse>> GetPaymentsAdjustmentsByFunderIdAndDateAsync(List<int>? funderIds, DateTime startDate, DateTime endDate, ReportingDateRangeType rangeType, int accountInfoId, CancellationToken cancellationToken)
    {
        funderIds ??= [];

        var baseQuery = paymentAdjustmentsRepository.Query()
           .Where(x =>
           x.AccountInfoId == accountInfoId &&
           x.DateDeleted == null &&
           (
               (rangeType == ReportingDateRangeType.transactionDate &&
                x.TransactionDate.HasValue &&
                x.TransactionDate.Value.Date >= startDate.Date &&
                x.TransactionDate.Value.Date <= endDate.Date) ||
               (rangeType != ReportingDateRangeType.transactionDate &&
                x.PaymentOrAdjustmentDate.HasValue &&
                x.PaymentOrAdjustmentDate.Value.Date >= startDate.Date &&
                x.PaymentOrAdjustmentDate.Value.Date <= endDate.Date)
           )
       );

        // Apply funder filter only for partial selection (not Select All)
        if (funderIds.Any())
        {
            baseQuery = baseQuery.Where(x => funderIds.Contains(x.FunderId));
        }

        var joinedWithClaimStatus = baseQuery
          .Join(claimStatusReportingRepository.Query(),
           pa => pa.ClaimStatusId,
           cs => cs.claimStatusId,
           (pa, cs) => new
           {
               pa,
               cs
           });

        // Use left join on funder names so records with blank/null/N/A funders are included
        var paymentsAdjustments = await joinedWithClaimStatus
          .GroupJoin(
               funderIds.Any()
                   ? funderNameReportingRepository.Query().Where(p => funderIds.Contains(p.FunderId))
                   : funderNameReportingRepository.Query(),
           combined => combined.pa.FunderId,
           funderName => funderName.FunderId,
           (combined, funderNames) => new { combined, funderNames })
          .SelectMany(
           x => x.funderNames.DefaultIfEmpty(),
           (x, funderName) => new
           {
               funderName = funderName != null ? funderName.FunderName : "N/A",
               clientId = x.combined.pa.ClientId,
               clientFirstName = string.Empty,
               clientLastName = string.Empty,
               claimFrom = x.combined.pa.ClaimFrom,
               claimThrough = x.combined.pa.ClaimThrough,
               claimStatus = x.combined.cs.claimStatus ?? string.Empty,
               billedDate = x.combined.pa.BilledDate,
               transactionType = x.combined.pa.TransactionType,
               transactionDate = x.combined.pa.TransactionDate,
               reasonCode = x.combined.pa.ReasonCode,
               remarkCode = x.combined.pa.RemarkCode,
               paymentsOrAdjustmentsDate = x.combined.pa.PaymentOrAdjustmentDate,
               eftOrCheckNumber = x.combined.pa.EftOrCheckNumber,
               payment = x.combined.pa.Payment,
               adjustment = x.combined.pa.Adjustment,
               dateDeleted = x.combined.pa.DateDeleted,
               dateModified = x.combined.pa.DateModified,
               dateCreated = x.combined.pa.DateCreated,
           }).ToListAsync(cancellationToken);

        var result = new List<PaymentsAdjustmentsResponse>();

        result.AddRange(paymentsAdjustments.Where(x => x.dateDeleted == null)
            .Select(item => new PaymentsAdjustmentsResponse
            {
                FunderName = item.funderName,
                ClientId = item.clientId,
                ClientFirst = item.clientFirstName,
                ClientLast = item.clientLastName,
                ClaimFrom = item.claimFrom,
                ClaimThrough = item.claimThrough,
                ClaimStatus = item.claimStatus,
                BilledDate = item.billedDate,
                TransactionDate = item.transactionDate,
                TransactionType = PaymentTypes((ClaimTransactionType)item.transactionType),
                ReasonCode = PaymentTypes((ClaimTransactionType)item.transactionType) == "PAY" ? "" : item.reasonCode,
                RemarkCode = PaymentTypes((ClaimTransactionType)item.transactionType) == "PAY" || string.IsNullOrWhiteSpace(item.remarkCode) ? null : item.remarkCode,
                PaymentOrAdjustmentDate = item.paymentsOrAdjustmentsDate,
                EftOrCheckNumber = item.eftOrCheckNumber,
                Payment = item.payment,
                Adjustment = item.adjustment,
                DateCreated = item.dateCreated,
                DateModified = item.dateModified,
            }));

        return result;
    }
    public async Task<(List<ClaimFollowUpResponse> Data, int Total)> GetClaimFollowUpReportData(ClaimFollowUpRequestModel model, CancellationToken cancellationToken)
    {
        model.FunderIds ??= [];

        if (model.SortingModels == null || model.SortingModels.Count == 0)
        {
            model.SortingModels = new List<SortingModel>
                {
                    new SortingModel
                    {
                        Dir = "desc",
                        Field = "dateModified"
                    }
                };
        }


        var funderNameMap = await funderNameReportingRepository.Query()
       .Where(f => f.DateDeleted == null)
       .GroupBy(f => f.FunderId)
       .Select(g => g.First())
       .ToDictionaryAsync(
           f => f.FunderId,
           f => f.FunderName ?? string.Empty,
           cancellationToken);


        var clientNameMap = await clientNameReportingRepository.Query()
                .Where(c => c.DateDeleted == null)
                .ToDictionaryAsync(
                    c => c.ClientId,
                    c => new
                    {
                        First = c.ClientFirstName ?? string.Empty,
                        Last = c.ClientLastName ?? string.Empty
                    },
                    cancellationToken);

        var claimStatusMap = await claimStatusReportingRepository.Query()
            .ToDictionaryAsync(
                s => s.claimStatusId,
                s => s.claimStatus ?? string.Empty,
                cancellationToken);

        var paymentsAdjustment = await paymentAdjustmentsRepository.Query()
            .Where(pa =>
                pa.AccountInfoId == model.AccountInfoId &&
                pa.DateDeleted == null &&
                pa.TransactionDate.HasValue)
            .ToListAsync(cancellationToken);

        var paymentsAdjustmentByClaimId = paymentsAdjustment
            .GroupBy(x => x.ClaimId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var noteBase = await claimNoteRepository.Query()
            .Where(cn =>
                (model.FollowUpType == (int)ReportingClaimFollowUpType.active
                    ? cn.DateDeleted == null
                    : cn.DateDeleted != null)
                && cn.RemindDate.Date >= model.StartDate.Date
                && cn.RemindDate.Date <= model.EndDate.Date
                && cn.Note != null)
            .Join(claimRepository.Query(),
                cn => cn.ClaimId,
                claim => claim.Id,
                (cn, claim) => new { cn, claim })
            .ToListAsync(cancellationToken);

        noteBase = noteBase
            .Where(x => !string.IsNullOrWhiteSpace(x.cn.Note) && !string.IsNullOrWhiteSpace(x.cn.Note.Trim()))
            .ToList();

        if (noteBase.Count == 0)
            return (new List<ClaimFollowUpResponse>(), 0);

        var claimIds = noteBase.Select(x => x.claim.Id).Distinct().ToList();

        var paymentClaims = await paymentClaimRepository.Query()
        .Where(pc => pc.ClaimId.HasValue && claimIds.Contains(pc.ClaimId.Value) && pc.DateDeleted == null)
            .ToListAsync(cancellationToken);

        var pcByClaimId = paymentClaims
            .GroupBy(pc => pc.ClaimId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.DateCreated).FirstOrDefault());

        var claimVersions = await claimVersionRepository.Query()
            .Where(cv => claimIds.Contains(cv.ClaimId) && cv.DateDeleted == null)
            .ToListAsync(cancellationToken);

        var cvByClaimId = claimVersions
            .GroupBy(cv => cv.ClaimId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.DateCreated).FirstOrDefault());

        var pcIds = pcByClaimId.Values
            .Where(x => x != null)
            .Select(x => x!.Id)
            .Distinct()
            .ToList();

        var serviceLines = pcIds.Count == 0
            ? new List<PaymentClaimServiceLineEntity>()
            : await paymentClaimServiceLineRepository.Query()
                .Where(psc => psc.DateDeleted == null && psc.PaymentClaimId.HasValue && pcIds.Contains(psc.PaymentClaimId.Value))
                .ToListAsync(cancellationToken);

        var dosByPaymentClaimId = serviceLines
            .Where(s => s.PaymentClaimId.HasValue)
            .GroupBy(s => s.PaymentClaimId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Where(x => x.DateOfService.HasValue)
                      .Select(x => x.DateOfService)
                      .OrderBy(x => x)
                      .FirstOrDefault());

        var providerIds = noteBase
            .Select(x =>
                (x.claim.RenderingStaffMemberId == -2 || x.claim.RenderingStaffMemberId == null)
                    ? x.claim.MemberId
                    : x.claim.RenderingStaffMemberId.Value)
            .Distinct()
            .ToList();

        var providers = await claimSearchRenderingProvidersRepository.Query()
            .AsNoTracking()
            .Where(r => r.DateDeleted == null && providerIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var providerNameMap = providers
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty);

        var models = noteBase
            .GroupBy(x => x.cn.Id)
            .Select(g =>
            {
                var first = g.First();
                var claimId = first.claim.Id;
                if (!paymentsAdjustmentByClaimId.TryGetValue(claimId, out var paRowsForClaim))
                    paRowsForClaim = new List<PaymentsAdjustmentsEntity>();

                if (model.FunderIds.Any())
                {
                    var claimMatches = model.FunderIds.Contains(first.claim.PrimaryFunderId);
                    var paMatches = paRowsForClaim.Any(y => model.FunderIds.Contains(y.FunderId));

                    if (!claimMatches && !paMatches)
                        return null;
                    if (paMatches)
                        paRowsForClaim = paRowsForClaim.Where(y => model.FunderIds.Contains(y.FunderId)).ToList();
                    else
                        paRowsForClaim = new List<PaymentsAdjustmentsEntity>();
                }

                var pa = paRowsForClaim
                    .OrderByDescending(x => x.DateModified)
                    .FirstOrDefault();
                pcByClaimId.TryGetValue(claimId, out var pcRow);
                cvByClaimId.TryGetValue(claimId, out var cvRow);
                var totalCharge = pcRow?.TotalCharge ?? 0m;
                var totalPayment = pcRow?.TotalPayment ?? 0m;
                var memberId = cvRow?.MemberId ?? 0;
                var placeOfService = cvRow?.PlaceOfService ?? string.Empty;
                var authorization = cvRow?.AuthorizationNumber ?? string.Empty;

                DateTime? dateOfService = null;
                if (pcRow != null && dosByPaymentClaimId.TryGetValue(pcRow.Id, out var dos))
                    dateOfService = dos;
                var renderingProviderId =
                    (first.claim.RenderingStaffMemberId == -2 || first.claim.RenderingStaffMemberId == null)
                        ? first.claim.MemberId
                        : first.claim.RenderingStaffMemberId.Value;
                providerNameMap.TryGetValue(renderingProviderId, out var renderingProviderName);
                renderingProviderName ??= string.Empty;
                decimal adjustment = 0m;
                decimal patientPayment = 0m;
                decimal balance = 0m;
                if (paRowsForClaim.Count > 0)
                {
                    adjustment = paRowsForClaim.Sum(z => z.Adjustment);
                    patientPayment = paRowsForClaim.Sum(z => z.Payment);
                    balance = totalCharge + adjustment - patientPayment;
                }
                var resolvedClientId = pa != null ? pa.ClientId : first.claim.ChildProfileId;
                string clientFirst = string.Empty;
                string clientLast = string.Empty;
                if (clientNameMap.TryGetValue(resolvedClientId, out var client))
                {
                    clientFirst = client.First;
                    clientLast = client.Last;
                }
                else if (pcRow != null)
                {
                    clientFirst = pcRow.ClientFirstName ?? string.Empty;
                    clientLast = pcRow.ClientLastName ?? string.Empty;
                }
                var resolvedFunderId = pa != null ? pa.FunderId : first.claim.PrimaryFunderId;
                funderNameMap.TryGetValue(resolvedFunderId, out var funderName);
                funderName ??= string.Empty;
                var claimStatus = string.Empty;
                if (pa != null)
                {
                    claimStatusMap.TryGetValue(pa.ClaimStatusId, out claimStatus);
                    claimStatus ??= string.Empty;
                }
                else
                {
                    claimStatus = first.claim.ClaimStatus.ToString();
                }
                var paymentDateCreated = pa != null ? pa.DateCreated : first.cn.DateCreated;
                var dateModified = pa != null ? pa.DateModified : first.cn.DateCreated;
                return new ClaimFollowUpReportModel
                {
                    Id = first.cn.Id,
                    ClaimId = first.claim.ClaimIdentifier,
                    ClaimIdValue = first.claim.Id,
                    MemberId = memberId,
                    DateDeleted = first.cn.DateDeleted,
                    Status = first.cn.DateDeleted == null ? 0 : 1,
                    PaymentAdjustmentId = pa != null ? pa.Id : 0,
                    ClientId = resolvedClientId,
                    FunderName = funderName,
                    ClaimStatus = claimStatus,
                    PaymentDateCreated = paymentDateCreated,
                    DateModified = dateModified,
                    ClaimFrom = pa != null ? pa.ClaimFrom : null,
                    ClaimThrough = pa != null ? pa.ClaimThrough : null,
                    ClientFirst = clientFirst,
                    ClientLast = clientLast,
                    RenderingProvider = renderingProviderName,
                    PlaceOfService = placeOfService,
                    Authorization = authorization,
                    ExpectedAmount = totalCharge,
                    BilledAmount = totalCharge,
                    PaymentAmount = totalPayment,
                    AdjustmentAmount = adjustment,
                    Balance = balance,
                    BilledDate = first.claim.billedDate,
                    Note = first.cn.Note,
                    CreatedBy = first.cn.CreatedBy,
                    NoteCreatedDate = first.cn.DateCreated,
                    FollowUpDate = first.cn.RemindDate,
                    DateOfService = dateOfService,
                    FollowUpStatus = first.cn.DateDeleted == null ? "Active" : "Completed",
                    NoteCreatedByName = string.Empty
                };
            })
            .Where(x => x != null)!
            .ToList();

        var total = models.Count;
        var paged = models.AsQueryable()
            .OrderBy(model.SortingModels)
            .Skip(model.Skip);
        var pageData = model.Take > 0
            ? paged.Take(model.Take).ToList()
            : paged.ToList();
        var result = pageData.Select(item => new ClaimFollowUpResponse
        {
            Id = item.Id,
            MemberId = item.MemberId,
            ClaimId = item.ClaimId,
            ClaimIdValue = item.ClaimIdValue,
            ClientFirst = item.ClientFirst,
            ClientLast = item.ClientLast,
            FunderName = item.FunderName,
            RenderingProvider = item.RenderingProvider,
            PlaceOfService = item.PlaceOfService,
            ClaimFrom = item.ClaimFrom ?? DateTime.Now,
            ClaimThrough = item.ClaimThrough ?? DateTime.Now,
            Authorization = item.Authorization,
            ExpectedAmount = item.ExpectedAmount,
            BilledAmount = item.BilledAmount,
            PaymentAmount = item.PaymentAmount,
            AdjustmentAmount = item.AdjustmentAmount,
            Balance = item.Balance,
            BilledDate = item.BilledDate,
            ClaimStatus = item.ClaimStatus,
            Note = item.Note,
            NoteCreatedBy = item.CreatedBy,
            DateOfService = item.DateOfService,
            NoteCreatedDate = item.NoteCreatedDate.ToString("MM/dd/yyyy"),
            FollowUpDate = item.FollowUpDate?.ToString("MM/dd/yyyy"),
            FollowUpStatus = item.FollowUpStatus,
            DateCreated = item.PaymentDateCreated,
            DateModified = item.DateModified,
            NoteCreatedByName = item.NoteCreatedByName,
            DateDeleted = item.DateDeleted
        }).ToList();

        return (result, total);
    }

    public string PaymentTypes(ClaimTransactionType transactionType)
    {
        string paymentType;
        switch (transactionType)
        {
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                paymentType = "PAY";
                break;
            default:
                paymentType = "ADJ";
                break;
        }
        return paymentType;
    }


    public Cell AddCell(ExcelCellType type, dynamic value, bool isAlternateRowColor = false)
    {
        string colorStyleIndex = isAlternateRowColor ? "2" : "0";
        type = value == null ? default : type;
        switch (type)
        {
            case ExcelCellType.number:
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)uint.Parse(colorStyleIndex)
                };

            case ExcelCellType.character:
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String,
                    StyleIndex = (UInt32Value)uint.Parse(colorStyleIndex)
                };

            case ExcelCellType.date:
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.Date,
                    StyleIndex = isAlternateRowColor ?
                        (UInt32Value)(int)ExcelCellDesignStyles.dateFormatWithBlueBackgroundStyle
                        : (UInt32Value)(int)ExcelCellDesignStyles.dateFormatWithClearBackgroundStyle
                };

            case ExcelCellType.decimalValue:
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.Number,
                    StyleIndex = isAlternateRowColor ?
                        (UInt32Value)(int)ExcelCellDesignStyles.twoDecimalValuesWithClearBackgroundStyle
                        : (UInt32Value)(int)ExcelCellDesignStyles.twoDecimalValuesWithBlueBackgroundStyle
                };

            case ExcelCellType.negativeDecimal:
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.Number,
                    StyleIndex = isAlternateRowColor ?
                        (UInt32Value)(int)ExcelCellDesignStyles.usStandardForNegativeNumberWithBlueBackgroundStyle
                        : (UInt32Value)(int)ExcelCellDesignStyles.usStandardForNegativeNumberWithClearBackgroundStyle
                };

            case ExcelCellType.header:
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String,
                    StyleIndex = (UInt32Value)(int)ExcelCellDesignStyles.headerCellStyles
                };

            case ExcelCellType.amount:
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.Number,
                    StyleIndex = isAlternateRowColor ?
                        (UInt32Value)(int)ExcelCellDesignStyles.twoDecimalValuesWithClearBackgroundStyle
                        : (UInt32Value)(int)ExcelCellDesignStyles.twoDecimalValuesWithBlueBackgroundStyle
                };

            default:
                return new Cell()
                {
                    CellValue = new CellValue(""),
                    DataType = CellValues.String,
                    StyleIndex = isAlternateRowColor ?
                        (UInt32Value)(int)ExcelCellDesignStyles.dateFormatWithBlueBackgroundStyle
                        : (UInt32Value)(int)ExcelCellDesignStyles.dateFormatWithClearBackgroundStyle
                };
        }
    }
    public void DefineStyles(WorkbookPart workbookPart)
    {
        var stylesheet = new Stylesheet();
        var fonts = new Fonts();
        fonts.Append(new Font(
            new FontSize() { Val = 10 },
            new FontName() { Val = "Calibri" }
        ));
        fonts.Append(new Font(
            new FontSize() { Val = 10 },
            new FontName() { Val = "Calibri" },
            new Bold()
        ));
        stylesheet.Append(fonts);

        var fills = new Fills();
        fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.None }));
        fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.LightGray, ForegroundColor = new ForegroundColor() { Rgb = "D9D9D9" } }));
        fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.Solid, ForegroundColor = new ForegroundColor() { Rgb = "D9E1F2" } }));
        fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.Solid, ForegroundColor = new ForegroundColor() { Rgb = "D9D9D9" } }));

        stylesheet.Append(fills);
        var borders = new Borders();
        borders.Append(new Border(
            new LeftBorder(),
            new RightBorder(),
            new TopBorder(),
            new BottomBorder(),
            new DiagonalBorder()
        ));
        stylesheet.Append(borders);

        var cellFormats = new CellFormats();
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.none, BorderId = 0, ApplyFill = true });//0: Font With black color and white background
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.lightGray, BorderId = 0, ApplyFill = true, });//1: Font With black color and gray background
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.lightBlue, BorderId = 0, ApplyFill = true });//2: Font With black color and light blue background
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.lightBlue, BorderId = 0, NumberFormatId = 40, ApplyNumberFormat = true, Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Right } });//3: Font With Excel Number Format 40(for red font, two decimal values and round brackets) and light blue background
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.none, BorderId = 0, NumberFormatId = 40, ApplyNumberFormat = true, Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Right } });//4: Font With Excel Number Format 40(for red font, two decimal values and round brackets) and white background
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.none, BorderId = 0, NumberFormatId = 4, ApplyNumberFormat = true, Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Right } });//5 :: TODO
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.none, BorderId = 0, NumberFormatId = 14, ApplyNumberFormat = true });//6 :Font With Excel Number Format 4(used for date format mm/dd/yyyy) and white background 
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.lightBlue, BorderId = 0, NumberFormatId = 14, ApplyNumberFormat = true });//7 :Font With Excel Number Format 4(used for date format mm/dd/yyyy) and light blue background 
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.lightBlue, BorderId = 0, NumberFormatId = 39, ApplyNumberFormat = true });//8 :Font With Excel Number Format 39(for black font color with two decimal values) and light blue background 
        cellFormats.Append(new CellFormat() { FontId = 0, FillId = (int)ExcelFillStyle.none, BorderId = 0, NumberFormatId = 39, ApplyNumberFormat = true });//9 :Font With Excel Number Format 39(for black font color with two decimal values) and white background 
        cellFormats.Append(new CellFormat() { FontId = 1, FillId = (int)ExcelFillStyle.none, BorderId = 0, ApplyFill = true });//10 :Font Bold styling and white background 

        stylesheet.Append(cellFormats);
        workbookPart.AddNewPart<WorkbookStylesPart>().Stylesheet = stylesheet;
    }


}
