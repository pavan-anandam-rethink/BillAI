using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.DTO;
using BillingService.Domain.Extensions;
using BillingService.Domain.Interfaces;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Templates.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Payment
{
    public class PaymentClaimService : BaseService, IPaymentClaimService
    {
        #region "Other APIs"
        private readonly IRepository<BillingDbContext, PaymentEntity> _paymentRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> _paymentClaimServiceLineAdjustmentRepository;
        private readonly IRepository<BillingDbContext, PaymentAdjustmentReasonEntity> _reasonCodesRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _claimChargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> _claimChargeEntryWriteOffRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimEntityRepository;
        private readonly IRepository<BillingDbContext, CarcCodeEntity> _carcCodeEntityRepository;
        private readonly IRepository<BillingDbContext, PatientInvoiceEntity> _patientInvoiceRepository;
        private readonly IRepository<BillingDbContext, PatientInvoiceDetailsEntity> _patientInvoiceDetailsRepository;
        private readonly IPaymentPostingService _paymentPostingService;
        private readonly IProviderBillingCodeService _providerBillingCodeService;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IChargeEntryService _chargeEntryService;
        private readonly IRazorViewService _razorViewService;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IRethinkMasterDataMicroServices _rethinkMasterDataMicroServices;
        private readonly IFileManagerService _fileManagerService;
        private readonly IPdfService _pdfService;
        private readonly IMessageBus _bus;
        private readonly IRepository<BillingDbContext, UnAllocatedPaymentEntity> _unAllocatedPaymentRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineErrorEntity> _paymentClaimServiceLineErrorEntity;

        public List<PaymentGroupedModel> _allChargeData = new List<PaymentGroupedModel>();

        private readonly ICacheService _cacheService;

        private readonly ConcurrentDictionary<string, List<PaymentGroupedModel>> _allChargeDataCacheDictionary = new ConcurrentDictionary<string, List<PaymentGroupedModel>>();

        private string GetCacheKey(int paymentId, int childProfileId) => $"{paymentId}_{childProfileId}";
        private Dictionary<int, PatientPaymentClaimFullModel> _groupedByChargeCache = null;

        // write-off cache
        private readonly Dictionary<int, decimal> _writeOffsCache = new Dictionary<int, decimal>();
        private readonly HashSet<int> _writeOffsCachedIds = new HashSet<int>();
        private List<PaymentGroupedModel> _allChargeDataCached;

        private string AllChargesKey(int paymentId, int childProfileId) => $"AllCharges_{paymentId}_{childProfileId}";
        private string GroupedKey(int paymentId, int childProfileId, bool isLinked) => $"Grouped_{paymentId}_{childProfileId}_{(isLinked ? 1 : 0)}";

        private static readonly TimeSpan cacheExpiration = TimeSpan.FromSeconds(120);
        private int _paymentId;

        private const string clientsCodeKey = "clientsCodeKey";
        public Task InvalidatePaymentCacheAsync(int paymentId, int childProfileId) => InvalidatePaymentCacheAsync(paymentId, new[] { childProfileId });
        private readonly ILogger<PaymentClaimService> _logger;

        public PaymentClaimService(
            IRepository<BillingDbContext, PaymentEntity> paymentRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustmentRepository,
            IPaymentPostingService paymentPostingService,
            IRepository<BillingDbContext, PaymentAdjustmentReasonEntity> reasonCodesRepository,
            IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository,
                IRepository<BillingDbContext, PatientInvoiceEntity> patientInvoiceRepository,
            IProviderBillingCodeService providerBillingCodeService,
            IClaimHistoryService claimHistoryService,
            IChargeEntryService chargeEntryService,
            IRazorViewService razorViewService,
            IClaimManagerService claimManagerService,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
            IRethinkMasterDataMicroServices rethinkMasterDataMicroServices,
            IRepository<BillingDbContext, ClaimEntity> claimEntityRepository,
            IFileManagerService fileManagerService,
            IPdfService pdfService,
            IMessageBus bus,
            IRepository<BillingDbContext, CarcCodeEntity> carcCodeEntityRepository,
            IRepository<BillingDbContext, UnAllocatedPaymentEntity> unAllocatedPaymentRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineErrorEntity> paymentClaimServiceLineErrorEntity,
             ICacheService cacheService,
            IRepository<BillingDbContext, PatientInvoiceDetailsEntity> patientInvoiceDetailsRepository,
             ILogger<PaymentClaimService> logger)
        {
            _claimEntityRepository = claimEntityRepository;
            _rethinkMasterDataMicroServices = rethinkMasterDataMicroServices;
            _claimChargeEntryRepository = claimChargeEntryRepository;
            _paymentRepository = paymentRepository;
            _paymentClaimRepository = paymentClaimRepository;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _paymentClaimServiceLineAdjustmentRepository = paymentClaimServiceLineAdjustmentRepository;
            _reasonCodesRepository = reasonCodesRepository;
            _paymentPostingService = paymentPostingService;
            _providerBillingCodeService = providerBillingCodeService;
            _claimHistoryService = claimHistoryService;
            _chargeEntryService = chargeEntryService;
            _razorViewService = razorViewService;
            _claimManagerService = claimManagerService;
            _fileManagerService = fileManagerService;
            _claimChargeEntryWriteOffRepository = claimChargeEntryWriteOffRepository;
            _pdfService = pdfService;
            _bus = bus;
            _carcCodeEntityRepository = carcCodeEntityRepository;
            _unAllocatedPaymentRepository = unAllocatedPaymentRepository;
            _paymentClaimServiceLineErrorEntity = paymentClaimServiceLineErrorEntity;
            _patientInvoiceRepository = patientInvoiceRepository;
            _cacheService = cacheService;
            _patientInvoiceDetailsRepository = patientInvoiceDetailsRepository;
            _logger = logger;
        }


        public async Task<List<AddPatientResponseModel>> CreatePaymentClaimsAsync(CreatePatientClaimsModel patientClaimsModel)
        {
            var openClaimsCount = 0;
            var response = new List<AddPatientResponseModel>();

            var invoicesQuery = _patientInvoiceRepository
                .Query()
                .Where(i => i.AccountId == patientClaimsModel.AccountInfoId && i.DateDeleted == null);

            var payment = await _paymentRepository.Query().FirstOrDefaultAsync(x => x.Id == patientClaimsModel.PaymentId);

            // call GetClientDetailsGuarantor to ensure payment is not null and belongs to the correct account
            List<int> clientIdsToFetch = invoicesQuery.Select(r => r.ClientId).Distinct().ToList();

            var clientDetails = await _rethinkMasterDataMicroServices.GetClientDetailsGuarantor(patientClaimsModel.AccountInfoId);

            //var guarantorDetails = clientDetails.Select(cd => cd.GuarantorContactId).Distinct().ToList();
            var guarantorDetails = clientDetails?.FirstOrDefault();
            var guarantorContactId = clientDetails?.FirstOrDefault()?.Id;

            for (int i = 0; i < patientClaimsModel.PatientIds.Length; i++)
            {
                var patientId = patientClaimsModel.PatientIds[i];

                decimal unAllocatedAmount = (patientClaimsModel.UnAllocatedAmount != null &&
                                             patientClaimsModel.UnAllocatedAmount.Length > i)
                                             ? patientClaimsModel.UnAllocatedAmount[i]
                                             : 0;

                string? note = (patientClaimsModel.Notes != null &&
                                patientClaimsModel.Notes.Length > i)
                                ? patientClaimsModel.Notes[i]
                                : null;

                var patientDetails = await _paymentClaimRepository.Query()
                    .Include(c => c.PaymentClaimServiceLines)
                    .Where(c => c.ChildProfileId == patientId && c.DateDeleted == null && c.PaymentClaimServiceLines
                        .Any(sl => sl.PaymentClaimServiceLineAdjustments.Any(adj => adj.AdjustmentGroupCode == "PR" && adj.DateDeleted == null)))
                    .ToListAsync();
                var patients = await _rethinkMasterDataMicroServices.GetChildProfile(patientClaimsModel.AccountInfoId, patientId);
                var patient = new ChildProfileEntityModel()
                {
                    Id = patients.id,
                    FirstName = patients.name.firstName,
                    MiddleName = patients.name.middleName,
                    LastName = patients.name.lastName,
                };

                var unAllocatedPaymentsModel = new UnAllocatedPaymentsModel
                {
                    PaymentId = patientClaimsModel.PaymentId,
                    ChildProfileId = patientId,
                    UnAllocatedAmount = unAllocatedAmount,
                    Notes = note,
                    AccountInfoId = patientClaimsModel.AccountInfoId,
                    MemberId = patientClaimsModel.MemberId,
                    GuarantorContactId = guarantorContactId
                };

                // Always attempt to add unallocated payments (even if patient not found in billing)
                await _paymentPostingService.AddUnAllocatedPayments(unAllocatedPaymentsModel);

                bool unallocatedAdded = unAllocatedAmount > 0;

                if (patientDetails.Any())
                {
                    var patientClaimsCharges = await _chargeEntryService.GetIdsAllOpenedPatientClaimAsync(patientId);
                    openClaimsCount += patientClaimsCharges.Count;

                    var patientClaimsIds = patientClaimsCharges.Select(x => x.ClaimId).ToList();

                    var paymentClaimsQuery = await GetPaymentClaimsByIdsAsync(patientClaimsModel.PaymentId, patientClaimsIds);
                    var paymentClaims = await paymentClaimsQuery.Include(x => x.Claim)
                                        .ToListAsync();


                    var existingClaimIds = paymentClaims.Where(x => x.DateDeleted == null).Select(x => x.ClaimId).ToHashSet();

                    int claimAttachedCount = 0;
                    // Filter out existing claims first
                    var newClaims = patientClaimsCharges.Where(c => !existingClaimIds.Contains(c.ClaimId)).ToList();

                    // Insert all new claims in a single batch
                    if (newClaims.Any())
                    {
                        claimAttachedCount += await CreatePaymentClaimsBatchAsync(
                            patientClaimsModel.PaymentId,
                            newClaims,
                            patient,
                            patientClaimsModel.MemberId
                        );
                    }
                    response.Add(new AddPatientResponseModel
                    {
                        patientId = patient.Id,
                        patientName = $"{patient.FirstName} {patient.MiddleName} {patient.LastName}",
                        // Consider unallocated credit as "attached" so the caller can see that something was added for this patient
                        isAttached = (claimAttachedCount > 0) || unallocatedAdded
                    });
                }
                else
                {
                    response.Add(new AddPatientResponseModel
                    {
                        patientId = patientId,
                        patientName = $"{patient.FirstName} {patient.MiddleName} {patient.LastName}",
                        // If there were no billable items, still mark as attached when an unallocated amount was added
                        isAttached = unallocatedAdded
                    });
                }
            }

            if (openClaimsCount > 0)
            {
                await _paymentClaimRepository.CommitAsync();
            }

            return response;
        }

        public async Task<int> CreateClaimsToEraAsync(CreateEraClaimsModel model)
        {
            var payment = await _paymentRepository.Query().Where(x => x.Id == model.PaymentId && x.DateDeleted == null).FirstOrDefaultAsync();

            if (payment == null)
            {
                return 0;
            }
            var claims = await _chargeEntryService.GetAllClaimsByIdAsync(payment, model.ClaimsIds);

            var claimsCount = 0;


            foreach (var claim in claims)
            {
                var paymentClaimExist = await (await _paymentClaimRepository.GetAllAsync(x =>
                    x.PaymentId == model.PaymentId && x.ClaimId == claim.ClaimId && x.DateDeleted == null)).Include(x => x.Claim).FirstOrDefaultAsync();

                if (paymentClaimExist != null)
                {
                    continue;
                }
                claimsCount += 1;

                //var accountInfoId = await _claimEntityRepository.Query().Where(x => x.Id == claim.ClaimId).Select(x => x.AccountInfoId).FirstOrDefaultAsync();

                var patients = await _rethinkMasterDataMicroServices.GetChildProfile(model.AccountInfoId, claim.PatientId);

                var patient = new ChildProfileEntityModel()
                {
                    Id = claim.PatientId,
                    FirstName = patients != null ? patients.name.firstName : "",
                    MiddleName = patients != null ? patients.name.middleName : "",
                    LastName = patients != null ? patients.name.lastName : "",
                    AccountInfoId = patients != null ? patients.accountId : model.AccountInfoId
                };

                //var patient = await _childProfileService.GetChildProfileById(claim.PatientId);
                int isClaimsAttached = await CreatePaymentClaimWithLines(model.PaymentId, claim, patient, model.MemberId);
            }
            await _paymentClaimRepository.CommitAsync();

            return claimsCount;
        }

        private async Task<int> CreatePaymentClaimsBatchAsync(int paymentId, List<ClaimChargeItem> claims, ChildProfileEntityModel patient, int memberId)
        {
            if (claims == null || claims.Count == 0) return 0;

            // 1. Load all claim identifiers in one go
            var claimIds = claims.Select(c => c.ClaimId).ToList();
            var claimIdentifiersLookup = _claimEntityRepository.Query()
                .Where(c => claimIds.Contains(c.Id))
                .ToDictionary(c => c.Id, c => c.ClaimIdentifier);

            var paymentClaims = new List<PaymentClaimEntity>();
            var serviceLines = new List<PaymentClaimServiceLineEntity>();
            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>();
            var histories = new List<ClaimHistorySaveModel>();

            foreach (var claim in claims)
            {
                var claimIdentifier = claimIdentifiersLookup.TryGetValue(claim.ClaimId, out var id) ? id : null;
                var totalCharges = claim.ChargeEntries.Sum(x => x.Charges);
                var totalPaymentOrig = claim.ChargeEntries.Sum(x => x.TotalAmount);

                var paymentClaim = new PaymentClaimEntity
                {
                    PaymentId = paymentId,
                    ChildProfileId = patient.Id,
                    ClaimStatus = claim.ClaimStatus.ToString(),
                    ClientFirstName = patient.FirstName,
                    ClientLastName = patient.LastName,
                    ClientMiddleName = patient.MiddleName,
                    ClaimId = claim.ClaimId,
                    ClaimIdentifier = claimIdentifier,
                    TotalCharge = totalCharges,
                    TotalChargeOrig = totalCharges,
                    TotalPayment = 0,
                    TotalPaymentOrig = totalPaymentOrig
                };

                MarkCreated(paymentClaim, memberId);
                paymentClaims.Add(paymentClaim);

                foreach (var ce in claim.ChargeEntries)
                {
                    var sl = new PaymentClaimServiceLineEntity
                    {
                        PaymentClaim = paymentClaim, // Navigation property, no DB call
                        ClaimChargeEntryId = ce.Id,
                        DateOfService = ce.DateOfService,
                        DateOfServiceOrig = ce.DateOfService,
                        ServiceCode = ce.ServiceCode ?? "",
                        ServiceCodeOrig = ce.ServiceCode ?? "",
                        PaymentAmount = 0,
                        PaymentAmountOrig = ce.TotalAmount,
                        ChargeAmount = ce.Charges,
                        ExpectedAmount = ce.Charges,
                        ChargeAmountOrig = ce.Charges,
                        ProcedureModifier1 = ce.Modifier1,
                        ProcedureModifier1Orig = ce.Modifier1,
                        ProcedureModifier2 = ce.Modifier2,
                        ProcedureModifier2Orig = ce.Modifier2,
                        ProcedureModifier3 = ce.Modifier3,
                        ProcedureModifier3Orig = ce.Modifier3,
                        ProcedureModifier4 = ce.Modifier4,
                        ProcedureModifier4Orig = ce.Modifier4,
                        ProcedureUnits = ce.Units.ToString(),
                        ProcedureUnitsOrig = ce.Units.ToString(),
                        ProcedureDesc = ce.Description
                    };

                    MarkCreated(sl, memberId);
                    serviceLines.Add(sl);

                    foreach (var adj in ce.ClaimChargeItems)
                    {
                        var adjEntity = new PaymentClaimServiceLineAdjustmentEntity
                        {
                            PaymentClaimServiceLine = sl, // Navigation property
                            AdjustmentAmount = adj.Amount,
                            AdjustmentGroupCode = "",
                            AdjustmentGroupCodeOrig = "",
                            AdjustmentReasonCode = adj.ReasonCodeId.ToString(),
                            AdjustmentReasonCodeOrig = ""
                        };

                        MarkCreated(adjEntity, 0);
                        adjustments.Add(adjEntity);
                    }
                }

                histories.Add(new ClaimHistorySaveModel
                {
                    ClaimId = claim.ClaimId,
                    MemberId = memberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.PaymentApplied,
                    ClaimHistoryAction = ClaimHistoryAction.PaymentAppliedManual,
                    NewValue = "",
                });
            }

            // 2. Bulk insert everything
            await _paymentClaimRepository.AddRangeAsync(paymentClaims);
            await _paymentClaimServiceLineRepository.AddRangeAsync(serviceLines);
            await _paymentClaimServiceLineAdjustmentRepository.AddRangeAsync(adjustments);

            // Insert claim history
            foreach (var h in histories)
            {
                await _claimHistoryService.AddAsync(h, false);
            }

            // Caller can commit once for the batch
            return paymentClaims.Count;
        }


        private async Task<int> CreatePaymentClaimWithLines(int paymentId, ClaimChargeItem claim, ChildProfileEntityModel patient, int memberId)
        {
            var paymentClaim = new PaymentClaimEntity
            {
                PaymentId = paymentId,
                ChildProfileId = patient.Id,
                ClaimStatus = claim.ClaimStatus.ToString(),
                ClientFirstName = patient.FirstName,
                ClientLastName = patient.LastName,
                ClientMiddleName = patient.MiddleName,
                ClaimId = claim.ClaimId,
                ClaimIdentifier = _claimEntityRepository.Query().FirstOrDefault(x => x.Id == claim.ClaimId)?.ClaimIdentifier,
                TotalCharge = claim.ChargeEntries.Sum(x => x.Charges),
                TotalChargeOrig = claim.ChargeEntries.Sum(x => x.Charges),
                TotalPayment = 0,
                TotalPaymentOrig = claim.ChargeEntries.Sum(x => x.TotalAmount)
                //TODO uncomment after group code will be implemented
                //PatientRespAmount = claim.ChargeEntries.Sum(charge => charge.ClaimChargeItems
                //    .Where(adj => adj.GroupCode == "RP").Sum(adj => adj.Amount))
            };

            MarkCreated(paymentClaim, memberId);
            var dbPaymentClaim = await _paymentClaimRepository.AddAndGetAsync(paymentClaim);
            dbPaymentClaim = await _paymentClaimRepository.Query().Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == dbPaymentClaim.Id);

            foreach (var claimChargeEntry in claim.ChargeEntries)
            {
                var chargeEntry = new PaymentClaimServiceLineEntity
                {
                    PaymentClaimId = dbPaymentClaim.Id,
                    ClaimChargeEntryId = claimChargeEntry.Id,
                    DateOfService = claimChargeEntry.DateOfService,
                    DateOfServiceOrig = claimChargeEntry.DateOfService,
                    ServiceCode = claimChargeEntry.ServiceCode ?? "",
                    ServiceCodeOrig = claimChargeEntry.ServiceCode ?? "",
                    PaymentAmount = 0,
                    PaymentAmountOrig = claimChargeEntry.TotalAmount,
                    ChargeAmount = claimChargeEntry.Charges,
                    ExpectedAmount = claimChargeEntry.Charges,
                    ChargeAmountOrig = claimChargeEntry.Charges,
                    ProcedureModifier1 = claimChargeEntry.Modifier1,
                    ProcedureModifier1Orig = claimChargeEntry.Modifier1,
                    ProcedureModifier2 = claimChargeEntry.Modifier2,
                    ProcedureModifier2Orig = claimChargeEntry.Modifier2,
                    ProcedureModifier3 = claimChargeEntry.Modifier3,
                    ProcedureModifier3Orig = claimChargeEntry.Modifier3,
                    ProcedureModifier4 = claimChargeEntry.Modifier4,
                    ProcedureModifier4Orig = claimChargeEntry.Modifier4,
                    ProcedureUnits = claimChargeEntry.Units.ToString(),
                    ProcedureUnitsOrig = claimChargeEntry.Units.ToString(),
                    ProcedureDesc = claimChargeEntry.Description

                };

                MarkCreated(chargeEntry, memberId);
                var dbServiceLine = await _paymentClaimServiceLineRepository.AddAndGetAsync(chargeEntry);

                foreach (var adjustment in claimChargeEntry.ClaimChargeItems)
                {
                    var chargeItem = new PaymentClaimServiceLineAdjustmentEntity
                    {
                        PaymentClaimServiceLineId = dbServiceLine.Id,
                        AdjustmentAmount = adjustment.Amount,
                        //TODO change it after claims process changes
                        AdjustmentGroupCode = "",
                        AdjustmentGroupCodeOrig = "",
                        AdjustmentReasonCode = adjustment.ReasonCodeId.ToString(),
                        AdjustmentReasonCodeOrig = "",
                    };

                    MarkCreated(chargeItem, 0);

                    //for delete reasons it must be equals on creating!
                    chargeItem.DateCreated = chargeEntry.DateCreated;
                    await _paymentClaimServiceLineAdjustmentRepository.AddAsync(chargeItem);
                }
                //}
            }

            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
            {
                ClaimId = claim.ClaimId,
                MemberId = dbPaymentClaim.Payment.CreatedBy,
                Mode = ClaimActionMode.User,
                ClaimAction = ClaimAction.PaymentApplied,
                ClaimHistoryAction = ClaimHistoryAction.PaymentAppliedManual,
                NewValue = $"{dbPaymentClaim.Payment.PaymentIdentifier}",
            }, false);
            return 1;
        }

        public async Task<PatientPaymentClaimsResponseModel> GetPatientPaymentClaims()
        {
            var result = await _paymentClaimRepository.Query().ToListAsync();
            return new PatientPaymentClaimsResponseModel();
        }


        public async Task<IQueryable<PaymentClaimEntity>> GetPaymentClaimsByIdsAsync(int paymentId, List<int> claimsIds)
        {
            var paymentClaims = await _paymentClaimRepository.GetAllAsync(x =>
                x.PaymentId == paymentId && x.ClaimId.HasValue && claimsIds.Contains(x.ClaimId.Value));

            return paymentClaims;
        }

        public async Task UpdatePaymentClaimServiceLineAmountsAsync(
            UpdatePaymentServiceLineAmountsModelWithUserInfo modelWithUserInfo)
        {
            int paymentType = 0;
            int claimId = 0;

            var serviceLine = await _paymentClaimServiceLineRepository.Query()
                .Include(x => x.PaymentClaim)
                    .ThenInclude(x => x.Payment)
                    .ThenInclude(x => x.UnallocatedPayments)
                .FirstOrDefaultAsync(x => x.Id == modelWithUserInfo.ServiceLineId);

            if (serviceLine?.PaymentClaim?.Payment?.UnallocatedPayments != null)
            {
                serviceLine.PaymentClaim.Payment.UnallocatedPayments =
                    serviceLine.PaymentClaim.Payment.UnallocatedPayments
                        .OrderByDescending(u => u.DateCreated)
                        .ToList();
            }

            var claim = serviceLine.PaymentClaim;
            var payment = serviceLine.PaymentClaim.Payment;
            var paidDifference = modelWithUserInfo.PaymentAmount - serviceLine.PaymentAmount;
            serviceLine.PaymentAmount = modelWithUserInfo.PaymentAmount;
            serviceLine.AllowedAmount = modelWithUserInfo.AllowedAmount;
            serviceLine.AllowedAmountOrig = modelWithUserInfo.AllowedAmount;
            MarkUpdated(serviceLine, modelWithUserInfo.MemberId);

            _paymentClaimServiceLineRepository.Update(serviceLine);

            if (serviceLine.PaymentClaimId != null)
            {
                claim.TotalPayment += paidDifference;

                MarkUpdated(claim, modelWithUserInfo.MemberId);
                _paymentClaimRepository.Update(claim);

                var isManual = payment.PaymentTypeId != (int)PaymentTypes.ERAReceived;
                paymentType = payment.PaymentTypeId;
                claimId = claim.ClaimId ?? 0;
            }
            var invoiceData = _patientInvoiceDetailsRepository.Query()
                .Where(x => x.ChargeId == serviceLine.ClaimChargeEntryId && x.DateDeleted == null).FirstOrDefault();

            var invoiceId = invoiceData?.InvoiceId ?? 0;

            if (invoiceId > 0)
            {
                // Get The data to update the invoice status
                var invoice = await _patientInvoiceRepository.Query().FirstOrDefaultAsync(x => x.Id == invoiceId);

                // Get All Charges belong to the invoice
                var invoiceDetails = await _patientInvoiceDetailsRepository.Query()
                    .Where(x => x.InvoiceId == invoiceId && x.DateDeleted == null).ToListAsync();

                // Update InvoiceDetail Balance
                var invoiceDetailsToUpdate = invoiceDetails.FirstOrDefault(x => x.ChargeId == serviceLine.ClaimChargeEntryId);
                if (invoiceDetailsToUpdate != null)
                {
                    invoiceDetailsToUpdate.PatientBalance = invoiceDetailsToUpdate.AdjustmentPatientResponsibility
                        - modelWithUserInfo.PaymentAmount;
                    invoiceDetailsToUpdate.PatientPayments = modelWithUserInfo.PaymentAmount;
                    MarkUpdated(invoiceDetailsToUpdate, modelWithUserInfo.MemberId);
                    _patientInvoiceDetailsRepository.Update(invoiceDetailsToUpdate);
                    await _patientInvoiceDetailsRepository.CommitAsync();
                }

                // update the Invoice Status base on all the changes and payments paid
                var invoiceDetailsToCalculate = await _patientInvoiceDetailsRepository.Query()
                    .Where(x => x.InvoiceId == invoiceId && x.DateDeleted == null)
                    .GroupBy(x => x.InvoiceId)
                    .Select(x => new
                    {
                        AdjustmentPatientResponsibility = x.Sum(y => y.AdjustmentPatientResponsibility),
                        PatientPayments = x.Sum(y => y.PatientPayments),
                        PatientBalance = x.Sum(y => y.PatientBalance)
                    }).FirstOrDefaultAsync();

                PatientInvoiceStatus newStatus = PatientInvoiceStatus.InvoiceSent;
                if (invoiceDetailsToCalculate.PatientBalance <= 0)
                {
                    newStatus = PatientInvoiceStatus.FullyPaid;
                }
                else if (invoiceDetailsToCalculate.PatientBalance > 0
                    && invoiceDetailsToCalculate.PatientPayments > 0
                    && invoiceDetailsToCalculate.PatientPayments < invoiceDetailsToCalculate.AdjustmentPatientResponsibility)
                {
                    newStatus = PatientInvoiceStatus.PartiallyPaid;
                }

                if (invoice != null && invoice.Status != newStatus)
                {
                    invoice.Status = newStatus;
                    MarkUpdated(invoice, modelWithUserInfo.MemberId);
                    _patientInvoiceRepository.Update(invoice);
                }
            }

            await _paymentClaimRepository.CommitAsync();
            // Fetching payment claim associated with the service line
            var paymentClaim = await _paymentClaimServiceLineRepository.Query()
                .Include(x => x.PaymentClaim)
                .ThenInclude(x => x.Claim)
                .FirstOrDefaultAsync(x => x.Id == modelWithUserInfo.ServiceLineId && x.DateDeleted == null);

            if (paymentClaim == null)
                return;

            // Fetching service lines using paymentClaimId 
            var serviceLineIds = await _paymentClaimServiceLineRepository.Query()
                .Where(x => x.PaymentClaimId == paymentClaim.PaymentClaimId && x.DateDeleted == null)
                .Select(x => x.Id)
                .ToListAsync();

            // Calculating total adjustment amount for those service lines
            var adjustmentAmountSum = await _paymentClaimServiceLineAdjustmentRepository.Query()
                .Where(x => serviceLineIds.Contains(x.PaymentClaimServiceLineId) && x.DateDeleted == null)
                .SumAsync(x => x.AdjustmentAmount);

            // Closing claim if total adjustment equals total charge
            if (paymentClaim.PaymentClaim.TotalCharge == adjustmentAmountSum + paymentClaim.PaymentClaim.TotalPayment)
            {
                paymentClaim.PaymentClaim.Claim.ClaimStatus = ClaimStatus.Closed;
                await _claimEntityRepository.UpdateAsync(paymentClaim.PaymentClaim.Claim);
                await _claimEntityRepository.SaveChangesAsync();
            }

            await InvalidatePaymentCacheAsync(claim.PaymentId, claim.ChildProfileId);
            await _bus.SendAsync(PrepareClaimTransaction(serviceLine.Id, (ClaimTransactionType)FindClaimTransactionTypeId((PaymentTypes)paymentType)), Topics.RT_Billing_ProcessClaimTxn);
        }

        public async Task PostPaymentClaimLines(PostPaymentClaimsModel model)
        {
            foreach (var claimLine in model.SelectedClaimLines)
            {
                if (claimLine.SelectedLines == null || claimLine.SelectedLines.Count == 0)
                {
                    claimLine.SelectedLines =
                        await (await _paymentClaimServiceLineRepository.GetAllAsync(x =>
                            x.PaymentClaimId == claimLine.ClaimId)).Select(x => new PostPaymentLineModel
                            {
                                Id = x.Id,
                                Balance = x.ChargeAmount -
                                      (x.PaymentClaimServiceLineAdjustments.Sum(y => y.AdjustmentAmount) ?? 0),
                                PaidAmount = x.PaymentAmount,
                                DateOfService = x.DateOfService,
                                PatientResponsibility = x.PaymentClaimServiceLineAdjustments
                                .Where(y => y.AdjustmentGroupCode == "PR")
                                .Sum(y => y.AdjustmentAmount) ?? 0,
                                Procedure = x.ServiceCode
                            }).ToListAsync();
                }

                if (claimLine.IsClaimSelected)
                {
                    var newPaymentId = await _paymentPostingService.PostManualPaymentAsync(model.PaymentId);
                    var paymentClaimEntity = await (await _paymentClaimRepository
                        .GetAllAsync(x => x.Id == claimLine.ClaimId)).FirstOrDefaultAsync();

                    paymentClaimEntity.PaymentId = newPaymentId;

                    _paymentClaimRepository.Update(paymentClaimEntity);
                }

                foreach (var claimServiceLine in claimLine.SelectedLines)
                {
                    var paymentClaimLine = await (await _paymentClaimServiceLineRepository
                            .GetAllAsync(x => x.Id == claimServiceLine.Id))
                        .Include(x => x.PaymentClaimServiceLineAdjustments).FirstOrDefaultAsync();

                    if (paymentClaimLine == null)
                    {
                        return;
                    }

                    var claimLineBalanceOld = paymentClaimLine.ChargeAmount -
                                              (paymentClaimLine.PaymentClaimServiceLineAdjustments
                                                  .Sum(y => y.AdjustmentAmount) ?? 0);

                    paymentClaimLine.PaymentAmount = claimServiceLine.PaidAmount;
                    paymentClaimLine.ChargeAmount =
                        paymentClaimLine.ChargeAmount - (claimLineBalanceOld - claimServiceLine.Balance);

                    _paymentClaimServiceLineRepository.Update(paymentClaimLine);
                }
            }

            await _paymentClaimServiceLineRepository.CommitAsync();
        }

        public async Task<string> PostPatientPaymentClaimLinesAsync(PostRemovePatientClaimsModel model)
        {
            var claimsToTransactionUpdate = new List<int>();
            var patientIds = new List<int>();
            model.PatientServiceLines.ForEach(x => patientIds.Add(x.PatientId));

            var paymentClaimsList = await (await GetPaymentPatientClaimEntitiesWithLinesAsync(model.PaymentId, patientIds))
                .ToListAsync();

            var currentPayment = paymentClaimsList.FirstOrDefault().Payment;
            var remainingAmountOrig = currentPayment.PaymentAmount - paymentClaimsList.Sum(pc => pc.TotalPayment ?? 0);
            var remainingAmount = currentPayment.PaymentAmount - paymentClaimsList.Sum(pc => pc.TotalPayment ?? 0);

            var chargePaymentsList = new List<ChargePaymentEntity>();
            foreach (var selectedPatientLines in model.PatientServiceLines)
            {
                //Getting lines
                var claimEntities = paymentClaimsList
                    .FindAll(x => x.ChildProfileId == selectedPatientLines.PatientId);

                if (selectedPatientLines.ServiceLines.Count == 0)
                {
                    foreach (var paymentClaimEntity in claimEntities)
                    {
                        foreach (var serviceLineEntity in paymentClaimEntity.PaymentClaimServiceLines)
                        {
                            selectedPatientLines.ServiceLines.Add(new ServiceLinePostDeleteModel
                            {
                                Id = serviceLineEntity.Id,
                                ClaimId = paymentClaimEntity.Id
                            });
                        }
                    }
                }

                var serviceLinesEntitiesToPost = new List<PaymentClaimServiceLineEntity>();
                selectedPatientLines.ServiceLines.ForEach(slModel =>
                {
                    var claimEntity = claimEntities.Find(pcEntity => pcEntity.Id == slModel.ClaimId);

                    var paymentClaimServiceLineEntity = claimEntity.PaymentClaimServiceLines
                        .FirstOrDefault(slEntity => slEntity.Id == slModel.Id && slEntity.PaymentClaimId == slModel.ClaimId);

                    serviceLinesEntitiesToPost.Add(paymentClaimServiceLineEntity);
                });

                var orderedServiceLinesEntitiesToPost = serviceLinesEntitiesToPost.OrderByApplicationType(model.PostingCriteriaId);

                //Posting lines process
                foreach (var serviceLineEntity in orderedServiceLinesEntitiesToPost)
                {
                    var currentClaimEntity = claimEntities.Find(x => x.Id == serviceLineEntity.PaymentClaimId);
                    var paymentClaimLinesToPostCount = selectedPatientLines.ServiceLines
                        .FindAll(x => x.ClaimId == serviceLineEntity.PaymentClaimId).Count;
                    var totalLinesBalance = currentClaimEntity.PaymentClaimServiceLines
                        .Sum(sl => sl.ChargeAmount) ?? 0
                        - currentClaimEntity.PaymentClaimServiceLines.Sum(sl =>
                            sl.PaymentClaimServiceLineAdjustments.Sum(adj =>
                            {
                                if (adj.AdjustmentGroupCode == "PR") return 0;
                                return adj.AdjustmentAmount;
                            }) ?? 0);

                    //close claim if fully paid
                    if (paymentClaimLinesToPostCount == currentClaimEntity.PaymentClaimServiceLines.Count && currentClaimEntity.ClaimId != null
                        && remainingAmountOrig == totalLinesBalance)
                    {
                        claimsToTransactionUpdate.Add((int)currentClaimEntity.ClaimId);
                        await _claimManagerService.UpdateClaimStatusAsync((int)currentClaimEntity.ClaimId, ClaimStatus.Closed, model.AccountInfoId, false);
                    }

                    if (serviceLineEntity == null) throw new ArgumentNullException("Service line not found");

                    var chargeEntryEntity = await _chargeEntryService.GetChargeEntityWithChargePaymentsAsync((int)serviceLineEntity.ClaimChargeEntryId, (int)currentClaimEntity.ClaimId);
                    if (chargeEntryEntity == null) throw new ArgumentNullException("Charge for service line not found");

                    var claimLineBalanceOld = serviceLineEntity.ChargeAmount -
                                              (serviceLineEntity.PaymentClaimServiceLineAdjustments
                                                  .Sum(y =>
                                                  {
                                                      if (y.AdjustmentGroupCode == "PR") return 0;
                                                      return y.AdjustmentAmount;
                                                  }) ?? 0) ?? 0;

                    if (remainingAmount <= 0) break;

                    var balanceDifference = Math.Clamp(remainingAmount, 0, claimLineBalanceOld);
                    remainingAmount -= balanceDifference;

                    //sl calculations
                    serviceLineEntity.PaymentAmount += balanceDifference;

                    var paymentClaimAdjustment = new PaymentClaimServiceLineAdjustmentEntity
                    {
                        AdjustmentAmount = balanceDifference,
                        PaymentClaimServiceLineId = serviceLineEntity.Id,
                        AdjustmentGroupCode = "CO",
                        AdjustmentGroupCodeOrig = "CO",
                        AdjustmentReasonCode = "27",
                        AdjustmentReasonCodeOrig = "27",

                        DateCreated = EstDateTime,
                        CreatedBy = model.AccountInfoId,
                        DateDeleted = null,
                        DateLastModified = EstDateTime,
                        ModifiedBy = model.AccountInfoId
                    };
                    _paymentClaimServiceLineAdjustmentRepository.Update(paymentClaimAdjustment);

                    //charge calculations
                    //increases chargeEntry payment amount
                    var chargePayment = new ChargePaymentEntity
                    {
                        Amount = balanceDifference,
                        ChargeId = chargeEntryEntity.Id,

                        //adjustment code
                        ReasonCodeId = 3,
                        PaymentMethodId = currentClaimEntity.Payment.PaymentMethodId,

                        DateCreated = EstDateTime,
                        CreatedBy = model.AccountInfoId,
                        DateDeleted = null,
                        DateLastModified = EstDateTime,
                        ModifiedBy = model.AccountInfoId
                    };
                    chargePaymentsList.Add(chargePayment);

                    //update payment amount in another payment claims connected to the same claim
                    //update total charge in another payment claims connected to the same claim to change balance
                    var paymentClaimsSLToUpdate = await (await _paymentClaimServiceLineRepository
                        .GetAllAsync(x => x.ClaimChargeEntryId == chargeEntryEntity.Id && x.DateDeleted == null)).ToListAsync();

                    paymentClaimsSLToUpdate.ForEach(sl =>
                    {
                        if (sl.Id != serviceLineEntity.Id)
                        {
                            sl.ChargeAmount = sl.ChargeAmount - balanceDifference;
                            //sl.PaymentAmountOrig = sl.ChargeAmount + balanceDifference;
                            sl.PaymentAmountOrig = sl.PaymentAmountOrig + balanceDifference;
                            sl.DateLastModified = EstDateTime;
                            _paymentClaimServiceLineRepository.Update(sl);

                            var pc = _paymentClaimRepository.GetByIdAsync((int)sl.PaymentClaimId).Result;
                            pc.TotalCharge = pc.TotalCharge - balanceDifference;
                            pc.TotalPaymentOrig = pc.TotalPaymentOrig + balanceDifference;
                            pc.DateLastModified = EstDateTime;
                            _paymentClaimRepository.Update(pc);
                        }
                    });
                    //payment claim calculations
                    currentClaimEntity.TotalPayment += balanceDifference;

                    _paymentClaimServiceLineRepository.Update(serviceLineEntity);
                    _paymentClaimRepository.Update(currentClaimEntity);

                }
            }

            var retryCount = 0;
            while (retryCount <= 4)
            {
                var maxChargePaymentId = await _chargeEntryService.GetMaxChargePaymentIdAsync();
                chargePaymentsList.ForEach(chargePaymentEntity =>
                {
                    maxChargePaymentId++;

                    chargePaymentEntity.Id = maxChargePaymentId;
                    _chargeEntryService.AddChargePaymentAsync(chargePaymentEntity, false);
                });

                try
                {
                    await _paymentClaimRepository.CommitAsync();
                    retryCount = 5;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount == 5) throw new Exception(ex.Message);
                }
            }

            return claimsToTransactionUpdate.Count == 0 ? String.Empty : String.Join(",", claimsToTransactionUpdate.ToArray());
        }

        public async Task<byte[]> GetEOBPaymentClaimPDFAsync(GetEOBClaimsModel model)
        {
            var paymentEOB = await _paymentPostingService.GetEOBPaymentInfoAsync(model.PaymentId);
            var claimEOB = await GetEOBClaimsAsync(model.PaymentId, model.Claims);

            //filter for eob details or errors tab
            if (model.ShowErrors)
            {
                foreach (var claim in claimEOB)
                {
                    claim.ServiceLines = claim.ServiceLines.Where(sl => sl.HasErrors && model.ShowErrors).ToList();
                }
            }
            else
            {
                foreach (var claim in claimEOB)
                {
                    claim.ServiceLines = claim.ServiceLines.Where(sl => !sl.HasErrors || !model.ShowErrors).ToList();
                }
            }

            var viewModel = new PaymentClaimEOBViewModel
            {
                PaymentEOB = paymentEOB,
                ClaimEOBInfo = claimEOB,
                CurrentTime = model.CurrentUserDateTime,
                ShowErrors = model.ShowErrors
            };

            var template = await _razorViewService.RenderViewToStringAsync("PaymentClaimView", viewModel);
            return await _pdfService.GeneratePDF(template);
        }

        public async Task<List<ClaimEOBInfoModel>> GetEOBClaimsAsync(int paymentId, List<int> claimIds)
        {
            if (claimIds != null && claimIds.Count == 0)
                claimIds = null;

            var reasonCodes = await _reasonCodesRepository.Query().ToListAsync();

            var deniedReasonCodes = await _carcCodeEntityRepository.Query().ToListAsync(); //await _claimService.GetAllCarcCodes();

            var locationCodes = await _rethinkMasterDataMicroServices.GetLocationCodes();

            var claims = await _paymentClaimRepository.Query()
                    .Include(x => x.Claim).ThenInclude(x => x.ClaimChargeEntries)
                    .Include(x => x.PaymentClaimServiceLines)
                    .Include("PaymentClaimServiceLines.PaymentClaimServiceLineAdjustments")
                    .Include("PaymentClaimServiceLines.PaymentClaimServiceLineErrors")
                .Where(x => x.DateDeleted == null && x.PaymentId == paymentId && (claimIds == null || claimIds.Contains(x.Id)))
                .Select(x => new ClaimEOBInfoModel
                {
                    Id = x.Id,
                    PatientId = x.PatientId,
                    PatientName = x.ClientFirstName + " " +
                                (x.ClientMiddleName != null ? x.ClientMiddleName + " " : "") + x.ClientLastName,
                    ClaimIdentifier = x.ClaimIdentifier,
                    BilledAmount = x.TotalCharge ?? 0,
                    PaidAmount = x.TotalPayment ?? 0,
                    PatientResponsibility = x.PatientRespAmount ?? 0,
                    Status = GetStatus(x.ClaimStatus),
                    AllowedAmount = x.PaymentClaimServiceLines.Sum(y => y.AllowedAmount),
                    PayerClaimNumber = x.ControlNumber != null ? x.ControlNumber.ToString() : "",
                    ClaimId = x.Claim != null ? x.Claim.Id : 0,
                    POSCode = x.PlaceOfService,
                    AccountInfoId = x.Claim != null ? x.Claim.AccountInfoId : (x.Payment != null ? x.Payment.AccountInfoId.GetValueOrDefault() : 0),
                    ProviderId = x.RenderingProviderId,
                    ProviderName = x.RenderingProviderName,
                    ClaimDateFrom = x.ClaimDateFrom,
                    ClaimDateTo = x.ClaimDateTo,

                    ServiceLines = x.PaymentClaimServiceLines
                        .Where(sl => sl.DateDeleted == null)
                        .Select(sl => new PaymentClaimServiceLineModel
                        {
                            Id = sl.Id,
                            DateOfService = sl.DateOfService,
                            Procedure = sl.ServiceCode,
                            AllowedAmount = sl.AllowedAmount,
                            BilledAmount = sl.ChargeAmount,
                            PaidAmount = sl.PaymentAmount,
                            PatientResponsibility = sl.PaymentClaimServiceLineAdjustments.Where(y => y.AdjustmentGroupCode == "PR").Sum(y => y.AdjustmentAmount) ?? 0,
                            Balance = sl.ChargeAmount - (sl.PaymentClaimServiceLineAdjustments.Sum(y => y.AdjustmentAmount) ?? 0),
                            DateLastModified = x.DateLastModified,
                            ClaimId = sl.PaymentClaimId ?? 0,
                            Units = Convert.ToDecimal(sl.ProcedureUnits),
                            ReasonCode = sl.PaymentClaimServiceLineAdjustments.Any()
                            ? sl.PaymentClaimServiceLineAdjustments.Select(x => x.AdjustmentReasonCode).ToList() : new List<string>(),
                            GroupCode = sl.PaymentClaimServiceLineAdjustments.Any()
                            ? sl.PaymentClaimServiceLineAdjustments.Select(x => x.AdjustmentGroupCode).ToList() : new List<string>(),
                            HasErrors = sl.PaymentClaimServiceLineErrors.Any() || sl.PaymentClaim.PaymentClaimErrors.Any(),
                            Adjustments = sl.PaymentClaimServiceLineAdjustments.ToList()
                        }).OrderByDescending(x => x.DateOfService).ToList()
                })
                .ToListAsync();

            foreach (var cl in claims)
            {
                var placeOfService = locationCodes.FirstOrDefault(p => p.code == cl.POSCode);
                cl.PlaceOfService = placeOfService != null ? $"{placeOfService.code} - {placeOfService.description}" : "";
                foreach (var sl in cl.ServiceLines)
                {
                    sl.Adjustment = sl.Adjustments.Where(y => y.AdjustmentGroupCode != "PR" && y.IsAdjustmentPositive == true).Sum(y => y.AdjustmentAmount) - sl.Adjustments.Where(y => y.AdjustmentGroupCode != "PR" && (y.IsAdjustmentPositive == false || y.IsAdjustmentPositive == null)).Sum(y => y.AdjustmentAmount);

                    var deductibleAmount = sl.Adjustments.Where(x => x.AdjustmentGroupCode == "PR" && x.AdjustmentReasonCode == "1").ToList();
                    sl.DeductibleAmount = deductibleAmount.Where(x => x.IsAdjustmentPositive == true).Sum(x => x.AdjustmentAmount) - deductibleAmount.Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)).Sum(x => x.AdjustmentAmount);

                    var coPayCoInsAmount = sl.Adjustments.Where(x => x.AdjustmentGroupCode == "PR" && (x.AdjustmentReasonCode == "2" || x.AdjustmentReasonCode == "3")).ToList();
                    sl.CoPayCoInsAmount = coPayCoInsAmount.Where(x => x.IsAdjustmentPositive == true).Sum(x => x.AdjustmentAmount) - coPayCoInsAmount.Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)).Sum(x => x.AdjustmentAmount);

                    sl.ReasonCodeData = new List<ReasonCodeData>();
                    for (int i = 0; i < sl.ReasonCode.Count(); i++)
                    {
                        var rsData = new ReasonCodeData();
                        if (cl.Status != ClaimStatus.Denied.ToString()
                            && reasonCodes.Any(c => c.AdjustmentCode == sl.ReasonCode[i] && c.GroupCode == sl.GroupCode[i]))
                        {
                            var reasonCode = reasonCodes.First(r => r.AdjustmentCode == sl.ReasonCode[i]);
                            rsData.ReasonCode = sl.ReasonCode[i];
                            rsData.CombinedCode = $"{reasonCode.GroupCode}: ({reasonCode.AdjustmentCode})";
                            rsData.Description = reasonCode.Description.Replace($"{reasonCode.GroupCode}: ({reasonCode.AdjustmentCode}) ", "");
                        }
                        // new implementation for denied reason codes
                        else if (cl.Status == ClaimStatus.Denied.ToString())
                        {
                            var reasonCode = reasonCodes.First(r => r.AdjustmentCode == sl.ReasonCode[i]);
                            rsData.ReasonCode = sl.ReasonCode[i];
                            rsData.CombinedCode = $"{sl.GroupCode[i]}: ({sl.ReasonCode[i]})";
                            rsData.Description = deniedReasonCodes.FirstOrDefault(x => x.Code == sl.ReasonCode[i]).Description;
                        }
                        else
                        {
                            rsData.ReasonCode = sl.ReasonCode[i];
                            rsData.CombinedCode = $"{sl.GroupCode[i]}: ({sl.ReasonCode[i]})";
                            rsData.Description = $"{sl.GroupCode[i]}: ({sl.ReasonCode[i]})";
                        }
                        sl.ReasonCodeData.Add(rsData);
                    }

                }
            }

            return claims;
        }

        public async Task<PaymentClaimErrorsResponseModel> GetPaymentClaimErrorsAsync(GetByIdSortFilterWithUserInfo model)
        {
            var selectQuery = _paymentClaimRepository.Query()
                .Where(x => x.PaymentId == model.Id && x.PaymentClaimErrors.Any())
                .Select(x => new PaymentClaimErrorModel
                {
                    Id = x.Id,
                    PatientId = x.ChildProfileId,
                    PatientName = x.ClientFirstName + " " +
                                  (x.ClientMiddleName != null
                                      ? x.ClientMiddleName + " "
                                      : "") + x.ClientLastName,
                    ClaimIdentifier = x.ClaimIdentifier,
                    AllowedAmount = x.PaymentClaimServiceLines.Sum(y => y.AllowedAmount) ?? 0,
                    ExpectedAmount = x.PaymentClaimServiceLines.Sum(y => y.ExpectedAmount) ?? 0,
                    Balance = x.TotalCharge - (x.PaymentClaimServiceLines
                        .SelectMany(y => y.PaymentClaimServiceLineAdjustments)
                        .Sum(z => z.AdjustmentAmount) ?? 0) ?? 0,
                    ErrorMessage = string.Join(", ", x.PaymentClaimErrors.Select(y => y.ErrorMessage))
                })
                .OrderBy(model.SortingModels);
            var totalCount = await selectQuery.CountAsync();
            var result = await selectQuery
                .Skip(model.Skip)
                .Take(model.Take)
                .ToListAsync();


            var response = new PaymentClaimErrorsResponseModel
            {
                Data = result,
                TotalCount = totalCount
            };

            return response;
        }


        public async Task RemoveSelectedClaimsAsync(RemovePaymentClaimsModel model)
        {
            var paymentClaimsList = await GetPaymentClaimEntitiesAsync(model.PaymentId, model.PaymentClaimsIds);
            List<ClaimTransactionModel> claimTransactionData = [];
            var payment = await _paymentRepository.Query().Where(x => x.Id == model.PaymentId).FirstOrDefaultAsync();

            foreach (var paymentClaim in paymentClaimsList)
            {
                foreach (var paymentClaimServiceLine in paymentClaim.PaymentClaimServiceLines
                    .Where(x => x.DateDeleted == null))
                {
                    foreach (var adjustments in paymentClaimServiceLine.PaymentClaimServiceLineAdjustments
                        .Where(x => x.DateDeleted == null))
                    {
                        SoftDelete(adjustments, model.MemberId);
                        _paymentClaimServiceLineAdjustmentRepository.Update(adjustments);
                        claimTransactionData.Add(PrepareClaimTransaction(adjustments.Id, (adjustments.AdjustmentGroupCode == "PR") ? ClaimTransactionType.patientResponsibility : (ClaimTransactionType)payment.PaymentTypeId));
                    }
                    claimTransactionData.Add(PrepareClaimTransaction(paymentClaimServiceLine.Id, ClaimTransactionType.deleteChargePayment));
                    SoftDelete(paymentClaimServiceLine, model.MemberId);
                    _paymentClaimServiceLineRepository.Update(paymentClaimServiceLine);
                }
                SoftDelete(paymentClaim, model.MemberId);
                _paymentClaimRepository.Update(paymentClaim);

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = paymentClaim.ClaimId ?? 0,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.PaymentRemoved,
                    ClaimHistoryAction = ClaimHistoryAction.PaymentUnapplied,
                    NewValue = $"{payment.PaymentIdentifier}",
                }, false);
            }
            await _paymentClaimServiceLineRepository.CommitAsync();
            await _paymentClaimServiceLineAdjustmentRepository.CommitAsync();
            await _paymentClaimRepository.CommitAsync();
            if (claimTransactionData.Count != 0)
            {
                await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
            }
        }

        public async Task RemoveSelectedPatientClaimsAsync(PostRemovePatientClaimsModel model)
        {
            var patientIds = new List<int>();
            List<ClaimTransactionModel> serviceLineIds = [];

            model.PatientServiceLines.ForEach(x => patientIds.Add(x.PatientId));

            var paymentClaimsList = await (await GetPaymentPatientClaimEntitiesWithLinesAsync(model.PaymentId, patientIds))
                .ToListAsync();
            var payment = await _paymentRepository.GetByIdAsync(paymentClaimsList.First().PaymentId);

            foreach (var selectedPatientLines in model.PatientServiceLines)
            {
                var claimEntities = paymentClaimsList.FindAll(x => x.ChildProfileId == selectedPatientLines.PatientId);

                if (selectedPatientLines.ServiceLines.Count == 0)
                {
                    foreach (var paymentClaimEntity in claimEntities)
                    {
                        SoftDelete(paymentClaimEntity, model.MemberId);
                        _paymentClaimRepository.Update(paymentClaimEntity);

                        await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                        {
                            ClaimId = paymentClaimEntity.ClaimId ?? 0,
                            MemberId = model.MemberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.PaymentRemoved,
                            ClaimHistoryAction = ClaimHistoryAction.PaymentUnapplied,
                            NewValue = $"{payment.PaymentIdentifier}",
                        }, false);

                        foreach (var paymentClaimSLEntity in paymentClaimEntity.PaymentClaimServiceLines)
                        {
                            SoftDelete(paymentClaimSLEntity, model.MemberId);
                            //paymentClaimSLEntity.ChargeAmount = paymentClaimSLEntity.ChargeAmount + paymentClaimSLEntity.PaymentAmount;
                            //paymentClaimSLEntity.PaymentAmountOrig = paymentClaimSLEntity.ChargeAmount - paymentClaimSLEntity.PaymentAmount;
                            //paymentClaimSLEntity.DateLastModified = EstDateTime;
                            _paymentClaimServiceLineRepository.Update(paymentClaimSLEntity);

                            var pc = _paymentClaimRepository.GetByIdAsync((int)paymentClaimSLEntity.PaymentClaimId).Result;
                            //pc.TotalCharge = pc.TotalCharge + paymentClaimSLEntity.PaymentAmount;
                            //pc.TotalPaymentOrig = pc.TotalPaymentOrig - paymentClaimSLEntity.PaymentAmount;
                            //pc.DateLastModified = EstDateTime;
                            _paymentClaimRepository.Update(pc);

                            foreach (var adjustment in paymentClaimSLEntity.PaymentClaimServiceLineAdjustments)
                            {
                                if (adjustment.DateCreated > paymentClaimEntity.DateCreated)
                                {
                                    //changes in charge entity
                                    var chargeEntryEntity =
                                        await _chargeEntryService.GetChargeEntityWithChargePaymentsAsync(
                                            (int)paymentClaimSLEntity.ClaimChargeEntryId, (int)paymentClaimEntity.ClaimId);
                                    if (chargeEntryEntity != null && chargeEntryEntity.ChargePayments != null)
                                    {
                                        var adjEntity = chargeEntryEntity.ChargePayments.ToList().Find(y =>
                                            y.Amount == adjustment.AdjustmentAmount && y.DateDeleted == null
                                                                                    && y.PaymentMethodId == payment.PaymentMethodId && y.ReasonCodeId == 3);

                                        if (adjEntity != null)
                                        {
                                            adjEntity.DateLastModified = EstDateTime;
                                            adjEntity.DateDeleted = EstDateTime;

                                            await _chargeEntryService.UpdateChargePaymentAsync(adjEntity);
                                        }
                                    }
                                }
                            }
                            _paymentPostingService.PrepareClaimTransactions(serviceLineIds, [paymentClaimSLEntity.Id], payment.PaymentTypeId);
                        }
                    }
                }

                foreach (var serviceLine in selectedPatientLines.ServiceLines)
                {
                    var currentPaymentClaimEntity = claimEntities.Find(x => x.Id == serviceLine.ClaimId);
                    var deletedWithClaim = false;
                    var paymentClaimLinesToDeleteCount = selectedPatientLines.ServiceLines
                        .FindAll(x => x.ClaimId == serviceLine.ClaimId).Count;

                    if (selectedPatientLines.ServiceLines.Count == 0 ||
                        currentPaymentClaimEntity.PaymentClaimServiceLines.Count == paymentClaimLinesToDeleteCount)
                    {
                        deletedWithClaim = true;
                        SoftDelete(currentPaymentClaimEntity, model.MemberId);
                        _paymentClaimRepository.Update(currentPaymentClaimEntity);
                    }

                    var serviceLineEntity = currentPaymentClaimEntity.PaymentClaimServiceLines
                        .FirstOrDefault(x => x.Id == serviceLine.Id
                                             && x.PaymentClaimId == serviceLine.ClaimId);

                    if (serviceLineEntity == null) throw new ArgumentNullException("service line not found");

                    SoftDelete(serviceLineEntity, model.MemberId);
                    _paymentClaimServiceLineRepository.Update(serviceLineEntity);

                    if (deletedWithClaim == false)
                    {
                        currentPaymentClaimEntity.TotalPayment -= serviceLineEntity.PaymentAmount;
                        currentPaymentClaimEntity.TotalCharge -= serviceLineEntity.ChargeAmount;

                        _paymentClaimRepository.Update(currentPaymentClaimEntity);
                    }

                    var serviceLineAdjustments = await (await _paymentClaimServiceLineAdjustmentRepository
                        .GetAllAsync(x => x.PaymentClaimServiceLineId == serviceLineEntity.Id && x.DateDeleted == null))
                        .ToListAsync();
                    foreach (var adjustment in serviceLineAdjustments)
                    {
                        if (adjustment.DateCreated > currentPaymentClaimEntity.DateCreated)
                        {
                            //changes in charge entity
                            var chargeEntryEntity =
                                await _chargeEntryService.GetChargeEntityWithChargePaymentsAsync(
                                    (int)serviceLineEntity.ClaimChargeEntryId, (int)currentPaymentClaimEntity.ClaimId);

                            var adjEntity = chargeEntryEntity.ChargePayments.FirstOrDefault(y =>
                                y.Amount == adjustment.AdjustmentAmount && y.DateDeleted == null
                                                                        && y.PaymentMethodId == payment.PaymentMethodId && y.ReasonCodeId == 3);

                            if (adjEntity != null)
                            {
                                adjEntity.DateLastModified = EstDateTime;
                                adjEntity.DateDeleted = EstDateTime;

                                await _chargeEntryService.UpdateChargePaymentAsync(adjEntity);
                            }
                        }
                    }
                    _paymentPostingService.PrepareClaimTransactions(serviceLineIds, [serviceLineEntity.Id], payment.PaymentTypeId);
                }
            }

            // SoftDelete the UnAllocated Payment if there are no more Patient associated with it
            var patientIdsToDeleteUnAllocatedPaymentAssciated = model.PatientServiceLines.Select(x => x.PatientId).ToList();
            var unAllocatedPayments = await _unAllocatedPaymentRepository.Query()
                .Where(x => x.PaymentId == model.PaymentId && patientIdsToDeleteUnAllocatedPaymentAssciated.Contains(x.ChildProfileId) && x.DateDeleted == null)
                .ToListAsync();

            unAllocatedPayments.ForEach(x => SoftDelete(x, model.MemberId));
            _unAllocatedPaymentRepository.UpdateRange(unAllocatedPayments);

            // Commit all transactions
            await _paymentClaimRepository.CommitAsync();
            await InvalidatePaymentCacheAsync(model.PaymentId, patientIds);
            await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, serviceLineIds);
        }

        public async Task RemoveSelectedPatientPaymentAmountsAsync(PostRemovePatientClaimsModel model)
        {
            var patientIds = new List<int>();
            List<ClaimTransactionModel> serviceLineIds = [];

            model.PatientServiceLines.ForEach(x => patientIds.Add(x.PatientId));

            var paymentClaimsList = await (await GetPaymentPatientClaimEntitiesWithLinesAsync(model.PaymentId, patientIds))
                .ToListAsync();
            var payment = await _paymentRepository.GetByIdAsync(paymentClaimsList.First().PaymentId);

            foreach (var selectedPatientLines in model.PatientServiceLines)
            {
                var claimEntities = paymentClaimsList.FindAll(x => x.ChildProfileId == selectedPatientLines.PatientId);

                if (selectedPatientLines.ServiceLines.Count == 0)
                {
                    foreach (var paymentClaimEntity in claimEntities)
                    {
                        foreach (var paymentClaimSLEntity in paymentClaimEntity.PaymentClaimServiceLines)
                        {
                            var paymentAmountDifference = paymentClaimSLEntity.PaymentAmount;
                            paymentClaimSLEntity.PaymentAmount = 0;
                            _paymentClaimServiceLineRepository.Update(paymentClaimSLEntity);

                            var pc = _paymentClaimRepository.GetByIdAsync((int)paymentClaimSLEntity.PaymentClaimId).Result;
                            pc.TotalPayment -= paymentAmountDifference;
                            _paymentClaimRepository.Update(pc);
                            _paymentPostingService.PrepareClaimTransactions(serviceLineIds, [paymentClaimSLEntity.Id], payment.PaymentTypeId);
                        }
                    }
                }

                foreach (var serviceLine in selectedPatientLines.ServiceLines)
                {
                    var currentPaymentClaimEntity = claimEntities.Find(x => x.Id == serviceLine.ClaimId);
                    var deletedWithClaim = false;
                    var paymentClaimLinesToDeleteCount = selectedPatientLines.ServiceLines
                        .FindAll(x => x.ClaimId == serviceLine.ClaimId).Count;

                    var serviceLineEntity = currentPaymentClaimEntity.PaymentClaimServiceLines
                        .FirstOrDefault(x => x.Id == serviceLine.Id
                                             && x.PaymentClaimId == serviceLine.ClaimId);

                    if (serviceLineEntity == null) throw new ArgumentNullException("service line not found");

                    var paymentDifference = serviceLineEntity.PaymentAmount;

                    serviceLineEntity.PaymentAmount = 0;
                    _paymentClaimServiceLineRepository.Update(serviceLineEntity);

                    if (deletedWithClaim == false)
                    {
                        currentPaymentClaimEntity.TotalPayment -= paymentDifference;

                        _paymentClaimRepository.Update(currentPaymentClaimEntity);
                    }


                    _paymentPostingService.PrepareClaimTransactions(serviceLineIds, [serviceLineEntity.Id], payment.PaymentTypeId);
                }
            }
            await _paymentClaimRepository.CommitAsync();
            await InvalidatePaymentCacheAsync(model.PaymentId, patientIds);
            await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, serviceLineIds);
        }

        public void UpdateWithoutSavePaymentClaim(PaymentClaimEntity paymentClaim)
        {
            _paymentClaimRepository.Update(paymentClaim);
        }

        public async Task<IQueryable<GetPaymentClaimServiceLinesSmall>> GetPaymentClaimServiceLinesSmallAsync(GetChargeDetailsModel model)
        {
            IQueryable<GetPaymentClaimServiceLinesSmall> result;
            if (model.IsServiceLine)
            {
                var serviceLine = await _paymentClaimServiceLineRepository.Query().FirstOrDefaultAsync(x => x.Id == model.Id);
                result = (await _paymentClaimServiceLineRepository.GetAllAsync(x =>
                    x.ClaimChargeEntryId == serviceLine.ClaimChargeEntryId && x.Id != serviceLine.Id && x.PaymentAmount != 0 && x.DateDeleted == null))
                .Include(x => x.PaymentClaim)
                    .ThenInclude(x => x.Payment)
                    .Where(x => x.PaymentClaim.DateDeleted == null && x.PaymentClaim.Payment.DateDeleted == null)
                .OrderBy(x => x.DateLastModified)
                .Select(x => new GetPaymentClaimServiceLinesSmall
                {
                    Id = x.Id,
                    PaymentId = x.PaymentClaim.PaymentId,
                    PaymentIdentifier = x.PaymentClaim.Payment.PaymentIdentifier,
                    AllowedAmount = x.AllowedAmount ?? 0,
                    PaidAmount = x.PaymentAmount ?? 0,
                    DateLastModified = x.DateLastModified,
                    PaymentType = x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.ClientPayment ? "Patient Payment" : (x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.InsurancePayment ? "Insurance Payment" : "Other Payment")
                });
            }
            else
            {
                result = (await _paymentClaimServiceLineRepository.GetAllAsync(x =>
                    x.ClaimChargeEntryId == model.Id && x.DateDeleted == null))
                .Include(x => x.PaymentClaim)
                    .ThenInclude(x => x.Payment)
                .OrderBy(x => x.DateLastModified)
                .Select(x => new GetPaymentClaimServiceLinesSmall
                {
                    Id = x.Id,
                    PaymentId = x.PaymentClaim.PaymentId,
                    PaymentIdentifier = x.PaymentClaim.Payment.PaymentIdentifier,
                    AllowedAmount = x.AllowedAmount ?? 0,
                    PaidAmount = x.PaymentAmount ?? 0,
                    DateLastModified = x.DateLastModified,
                    PaymentType = x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.ClientPayment ? "Patient Payment" : (x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.InsurancePayment ? "Insurance Payment" : "Other Payment")
                });
            }
            return result;
        }


        private async Task<List<PaymentClaimEntity>> GetPaymentClaimEntitiesAsync(int paymentId, int[] claimsIds)
        {
            var result = await _paymentClaimRepository.Query()
                .Where(x => x.PaymentId == paymentId && claimsIds.Contains(x.Id) && x.DateDeleted == null)
                .Include(x => x.PaymentClaimServiceLines)
                .ThenInclude(x => x.PaymentClaimServiceLineAdjustments).ToListAsync();
            //.GetAllAsync(x => x.PaymentId == paymentId && claimsIds.Contains(x.Id) && x.DateDeleted == null);

            return result;
        }

        private async Task<IQueryable<PaymentClaimEntity>> GetPaymentPatientClaimEntitiesWithLinesAsync(int paymentId, List<int> patientIds)
        {
            var result = (await _paymentClaimRepository
                .GetAllAsync(x => x.PaymentId == paymentId && patientIds.Contains(x.ChildProfileId)
                                                           && x.DateDeleted == null))
                .Include(x => x.PaymentClaimServiceLines)
                .ThenInclude(x => x.PaymentClaimServiceLineAdjustments)
                .Include(x => x.Payment);

            return result;
        }

        private async Task<decimal?> CalculateServiceLineExpectedAsync(int funderId, string serviceCode, decimal units, int accountInfoId)
        {
            var rate = await _providerBillingCodeService.GetServiceRateAsync(funderId, serviceCode, accountInfoId);
            return (rate.HasValue) ? (rate * units) : null;
        }

        public async Task<ChildProfileInfo> getPatientDetails(int patientId, int accountInfoId)
        {
            return await _claimManagerService.GetPatientInfoById(patientId, accountInfoId);
        }

        public async Task<ClientPrintData> GetCompanyAccountInfoByPatientId(GetClientPrintDataRequest model)
        {
            var accountInfo = await _rethinkMasterDataMicroServices.GetChildProfileReturningEntity(model.AccountInfoId, model.PatientId);
            accountInfo.AccountInfo = await _rethinkMasterDataMicroServices.GetAccountReturningEntityAsync(model.AccountInfoId);
            accountInfo.StateLU = await _rethinkMasterDataMicroServices.GetStateById(accountInfo.StateId ?? 0);
            accountInfo.CountryLU = await _rethinkMasterDataMicroServices.GetCountryById(accountInfo.CountryId ?? 0);
            if (accountInfo == null)
            {
                return new ClientPrintData { };
            }

            return new ClientPrintData
            {
                ClaimId = model.ClaimId,

                PatientId = model.PatientId,

                CompanyName = accountInfo.AccountInfo.AccountOrganizationName,

                CompanyAddress = $"{(!String.IsNullOrEmpty(accountInfo.AccountInfo.BillingAddress1) ? accountInfo.AccountInfo.BillingAddress1 + "," : "")}" +
                                    $"{(!String.IsNullOrEmpty(accountInfo.AccountInfo.BillingCity) ? accountInfo.AccountInfo.BillingCity + "," : "")}" +
                                    $"{(!String.IsNullOrEmpty(accountInfo.AccountInfo.BillingTown) ? accountInfo.AccountInfo.BillingTown + "," : "")}" +
                                    $"{(!String.IsNullOrEmpty(accountInfo.StateLU?.abbreviation) ? accountInfo.StateLU?.abbreviation + "," : "")}" +
                                    $"{(!String.IsNullOrEmpty(accountInfo.CountryLU?.name) ? accountInfo.CountryLU?.name + "," : "")}" +
                                    $"{accountInfo.AccountInfo.BillingZip}",

                CompanyPhones = $"{accountInfo.AccountInfo.BillingProviderPhone}",

                CompanyLogoUrl = await GetLogo(accountInfo.AccountInfo.ProviderLogo),

                CompanyEmail = accountInfo.AccountInfo.Email,

                ClientName = $"{accountInfo.FirstName} {accountInfo.MiddleName} {accountInfo.LastName}",

                ClientAddress = $"{(!String.IsNullOrEmpty(accountInfo.Address) ? accountInfo.Address + "," : "")}" +
                                $"{(!String.IsNullOrEmpty(accountInfo.City) ? accountInfo.City + "," : "")} " +
                                $"{(!String.IsNullOrEmpty(accountInfo.Town) ? accountInfo.Town + "," : "")} " +
                                $"{(!String.IsNullOrEmpty(accountInfo.StateLU?.abbreviation) ? accountInfo.StateLU?.abbreviation + "," : "")} " +
                                $"{(!String.IsNullOrEmpty(accountInfo.CountryLU?.name) ? accountInfo.CountryLU?.name + "," : "")} " +
                                $"{accountInfo.ZipCode}",

                PaymentPostingDate = EstDateTime,
                ClientAccountId = accountInfo.AccountInfoId.ToString(),
                TotalPayment = 455.55,
                Remaining = 10.05
            };
        }

        private async Task<string> GetLogo(string companyLogoUrl)
        {
            if (string.IsNullOrWhiteSpace(companyLogoUrl))
            {
                return _defaultLogo;
            }

            try
            {
                var dowmloadUrl = await _fileManagerService.GetFileUrl(companyLogoUrl);
                return await GetImageAsBase64Url(dowmloadUrl);
            }
            catch
            {
                return _defaultLogo;
            }
        }

        private readonly string _defaultLogo = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAANsAAACkCAMAAAApKatmAAAAMFBMVEX////BwcG/v7/f39/v7+/7+/vPz8/z8/Pn5+fLy8vHx8fX19fj4+P39/fr6+vT09OFCFGdAAAC+UlEQVR4nO2a63KDIBCFVUDxEvP+b9u4BAXRpOksK+mc709lRw+cLPdpVQEAAAAAAAAAAAAAAAAAAIDvpH7BlZVnloe3TJVnloe3TJWzyXMI8QFvkkJ8wJukEB/wJinEB7xJCvEBb5JCfMCbpBAf8CYpxAe8SQrxAW+SQnzAm6QQHwU2CQAAAAAAALCiJ6Wm7jisk3BnlVLaSDTs9+jmgdpHjaobYlCH4VpFNuzgws14y93eTzj01j0tLPRbc7shCG8pNe0Wrmehdv+GI2+htUdzzeuw6cNwU5C5A2+GPAyT1q4Lts+wy9qorBqCcNW6Tqr1ROG6nG554O1OFujR5cTSsyLH1BXNuIUt9VCXRArfxdr+jtSbDpPSkaHlibJZ+1HWryaGMFeUuXRyvYjU2xiNmrsvTdGL81JaHuyW5KR0Nak3ys9aWltLw2qb+VtlaZUj7+t6Z9Y0l0DirQu7ZFXdaDg9HvzfHbVPoIPGZymzSeLN7gKNa3x3Mk3sLMdpvJjEm9oFetcXj/cv8cTjv57yNfcj3nprXSaC95RHn3hLf4Jr+IM3vwNZiv/Am/nIW7F9cn4xl7RbxL/1VXPJbg0w4RrgVi694D/brQFt0WvAbu2mNC5rd7xyGf/ZLlFN9PHFvNtzraV4z7Wugt+159LhENr2yrRB8Yc2d96hz+ogn6b0vbIbM+7Hd8dRd5ihDLrDjLmvc4mbGZ/hfjdrXoxuIpaGdf5sOo9N0NgbhevRWn9r4n4SMrScTV24tLNp7M2NmpXeb/6jcDt4b/FVQzELQHXsrbLhXdB2rrFhdFq7cnhFVBcz2KoTb9VtfJYHG77c+RstZZZVwA/Ts6u9UjH6sRm2SRZu8yM8pw5OrmQBAAAAAAAA0hT4j8/4P3NJIT7gTVKID3iTFOID3iSF+IA3SSE+4E1SiA94kxTiA94khfiAN0khPuBNUogPeJMU4oPX2wkc+n+tPLM8vGWqPLM8vGWqPHvdAAAAAAAAAAAAAAAAAAD4An4A5QcTnXnPO7wAAAAASUVORK5CYII=";

        private async Task<string> GetImageAsBase64Url(string url)
        {

            using (var client = new HttpClient())
            {
                var bytes = await client.GetByteArrayAsync(url);
                return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
            }
        }

        #region "CalculatePaymentValuesByChargeId"
        private decimal getChargeEntryWriteOff(int chargeId)
        {
            if (chargeId == 0) return 0;
            var amount = _claimChargeEntryWriteOffRepository.Query().Where(x => x.ClaimChargeEntryId == chargeId && x.DateDeleted == null).Sum(x => x.WriteOffAmount);
            return amount ?? 0;
        }
        private decimal getClaimWriteOff(int claimId)
        {
            if (claimId == 0) return 0;
            var amount = _claimChargeEntryWriteOffRepository.Query().Include(x => x.ClaimWriteOff).Where(x => x.ClaimWriteOff.ClaimId == claimId && x.DateDeleted == null).Sum(x => x.WriteOffAmount);
            return amount ?? 0;
        }
        #endregion

        #endregion

        public async Task<List<PatientPaymentClaimFullModel>> GetGroupedByPayments(PaymentEntity payment, GroupByParam groupby, bool isLinked = false, int childProfileId = 0)
        {
            await GetAllCharges(payment.Id, childProfileId);
            if (!isLinked)
            {
                _allChargeData = _allChargeData.Where(x => x.DateOfService <= payment.DepositDate).ToList();
            }
            var response = await GroupByCharges(_allChargeData, groupby, false, payment);
            return response;
        }

        public async Task<List<PatientPaymentClaimFullModel>> GetGroupedByPayments(List<PaymentGroupedModel> allChargeData, PaymentEntity payment, GroupByParam groupby, bool isLinked = false)
        {
            if (!isLinked)
            {
                allChargeData = allChargeData.Where(x => x.DateOfService <= payment.DepositDate).ToList();
            }
            var response = await GroupByCharges(allChargeData, groupby, false, payment);

            return response;
        }

        public async Task<List<PatientPaymentClaimFullModel>> GetGroupedByPaymentsForPatientInvoice(List<int> chargeIds)
        {
            var data = await GetChargeInfoByIds(chargeIds);
            var response = await GroupByCharges(data, GroupByParam.Charge, true);
            return response;
        }

        public async Task<PaymentClaimModel> GetPaymentClaimAsync(int claimId)
        {
            var paymentClaim = await _paymentClaimRepository.Query().Where(x => x.Id == claimId).Include(x => x.Payment).FirstOrDefaultAsync();

            if (paymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.ClientPayment || paymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.OtherPayment)
            {
                var data = await GetPatientDetailsAsync(paymentClaim.ChildProfileId, paymentClaim.Payment.Id, false);

                if (data == null) return new PaymentClaimModel();

                var claimModel = new PaymentClaimModel();
                claimModel.PaymentId = data.PaymentId;
                claimModel.PatientId = data.PatientId;
                claimModel.PatientName = data.PatientName;
                claimModel.InsurancePayment = data.InsurancePayment;
                claimModel.PatientPayment = data.PatientPayment;
                claimModel.Adjustment = data.Adjustment;
                claimModel.PatientResponsibility = data.PatientResponsibility;
                claimModel.PatientResponsibilityBalance = data.PatientResponsibilityBalance;

                return claimModel;
            }
            else
            {
                var claim = await _paymentClaimRepository.Query()
                .Where(x => x.Id == claimId && x.DateDeleted == null && x.Claim.DateDeleted == null)
                .Select(x => new PaymentClaimModel
                {
                    Id = x.Id,
                    PaymentId = x.PaymentId,
                    PatientId = x.ChildProfileId,
                    PatientName = x.ClientFirstName + " " +
                                  (x.ClientMiddleName != null ? x.ClientMiddleName + " " : "") + x.ClientLastName,
                    ClaimId = x.ClaimId,
                    ClaimIdentifier = x.ClaimIdentifier,
                    DateOfServiceStart = x.Claim.ClaimChargeEntries.Min(c => c.DateOfService),
                    BilledAmount = x.TotalCharge ?? x.PaymentClaimServiceLines.Where(x => x.DateDeleted == null).Sum(y => y.ChargeAmount),
                    PaidAmount = x.TotalPayment ?? 0,
                    PatientResponsibility = x.PatientRespAmount ?? 0,
                    Status = GetStatus(x.ClaimStatus),
                    AllowedAmount = x.PaymentClaimServiceLines.Where(x => x.DateDeleted == null).Sum(y => y.AllowedAmount),
                    Balance = x.TotalCharge - (x.TotalPayment ?? 0)
                            + (x.PaymentClaimServiceLines.Where(x => x.DateDeleted == null).SelectMany(x => x.PaymentClaimServiceLineAdjustments)
                            .Where(a => a.AdjustmentGroupCode != "PR" && a.DateDeleted == null && a.IsAdjustmentPositive == true)
                            .Sum(lineAdj => lineAdj.AdjustmentAmount) ?? 0)
                            - (x.PatientRespAmount ?? 0),

                })
                .FirstOrDefaultAsync();

                var groupByData = await GetGroupedByPayments(paymentClaim.Payment, GroupByParam.Claim, true);
                var serviceLineData = groupByData.Where(x => x.ClaimId == claim.ClaimId).FirstOrDefault();
                claim.InsurancePayment = serviceLineData.InsurancePayment;
                claim.PaidAmount = serviceLineData.InsurancePayment;
                claim.PatientPayment = serviceLineData.PatientPayment;
                claim.Adjustment = serviceLineData.Adjustment;
                claim.PatientResponsibility = serviceLineData.PatientResponsibility;
                claim.PatientResponsibilityBalance = serviceLineData.PatientResponsibilityBalance;
                var totalWriteOffAmount = getClaimWriteOff(claim.ClaimId ?? 0);
                claim.Balance = claim.BilledAmount - claim.InsurancePayment + claim.Adjustment + claim.PatientResponsibility - totalWriteOffAmount;
                return claim;
            }
        }


        public async Task<PaymentClaimsResponseModel> GetPaymentClaimsAsync(GetClaimFilterModel getPaymentClaimsModel)
        {
            try
            {
                var query = _paymentClaimRepository.Query()
                            .Include(x => x.Claim)
                                .ThenInclude(c => c.ClaimSubmissions) // Include ClaimSubmissions
                            .Include(x => x.PaymentClaimServiceLines)
                                .ThenInclude(x => x.PaymentClaimServiceLineAdjustments)
                            .Where(x => x.PaymentId == getPaymentClaimsModel.PaymentId && x.DateDeleted == null);

                if (!string.IsNullOrWhiteSpace(getPaymentClaimsModel.FilterModels.ClientIds))
                {
                    var clientIds = getPaymentClaimsModel.FilterModels.ClientIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => id.Trim())
                        .Where(id => int.TryParse(id, out _))
                        .Select(int.Parse)
                        .ToList();
                    query = query.Where(x => clientIds.Contains(x.ChildProfileId));
                }

                if (!string.IsNullOrWhiteSpace(getPaymentClaimsModel.FilterModels.ClaimIdentifier))
                {
                    var claimIdFilter = getPaymentClaimsModel.FilterModels.ClaimIdentifier.Trim().ToLower();
                    query = query.Where(x => x.Claim.ClaimIdentifier != null && x.Claim.ClaimIdentifier.ToLower().Contains(claimIdFilter));
                }

                var projectedQuery = query.Select(x => new
                {
                    PaymentClaim = x,
                    LatestClaimSubmission = x.Claim.ClaimSubmissions
                                            .Where(cs => cs.DateDeleted == null)
                                            .OrderByDescending(cs => cs.Id)
                                            .FirstOrDefault()
                });

                var selectQuery = projectedQuery
                    .Where(result => result.PaymentClaim.Claim != null)
                    .Select(result => new PaymentClaimModel
                    {
                        Id = result.PaymentClaim.Id,
                        PaymentId = result.PaymentClaim.PaymentId,
                        PatientId = result.PaymentClaim.ChildProfileId,
                        PatientName = result.PaymentClaim.ClientFirstName + " " + (result.PaymentClaim.ClientMiddleName != null ? result.PaymentClaim.ClientMiddleName + " " : "") +
                                  result.PaymentClaim.ClientLastName,
                        ClaimId = result.PaymentClaim.ClaimId > 0 ? result.PaymentClaim.ClaimId : 0,
                        ClaimIdentifier = result.PaymentClaim.Claim != null ? result.PaymentClaim.Claim.ClaimIdentifier : result.PaymentClaim.ClaimIdentifier,
                        ClaimStatus = result.PaymentClaim.Claim != null ? GetClaimStatusDescription(result.PaymentClaim.Claim.ClaimStatus) : null,
                        DateOfServiceStart = result.PaymentClaim.Claim != null
                                         ? result.PaymentClaim.Claim.ClaimChargeEntries.Min(c => c.DateOfService) : DateTime.MinValue,
                        BilledAmount = result.PaymentClaim != null ? result.PaymentClaim.TotalCharge : 0,
                        Status = result.PaymentClaim.ClaimStatus != null
                            ? GetStatus(result.PaymentClaim.ClaimStatus)
                            : "Rejected",
                        AllowedAmount = result.PaymentClaim.PaymentClaimServiceLines.Where(y => y.DateDeleted == null).Sum(y => y.AllowedAmount) ?? 0,
                        IsFlagged = result.PaymentClaim.Claim != null ? result.PaymentClaim.Claim.IsFlagged : false,
                        CmsPageCount = result.PaymentClaim.Claim != null ? (result.PaymentClaim.Claim.ClaimChargeEntries.Count() + 6 - 1) / 6 : 0,
                        ClaimActionTypes = string.Join(",",
                                    result.PaymentClaim.PaymentClaimServiceLines
                                    .SelectMany(s => s.PaymentClaimServiceLineAdjustments)
                                    .Where(adj => adj.DateDeleted == null)
                                    .Select(adj => adj.Mode)
                                    .Distinct()
                        ),
                        isSecondaryPayerAvailable = result.PaymentClaim.Claim != null ? result.PaymentClaim.Claim.IsSecondaryPayerAvailable : false,
                        submissionTypeId = (int?)(result.LatestClaimSubmission.SubmissionType) ?? 0
                    });

                var result = await selectQuery.ToListAsync();

                if (getPaymentClaimsModel.SortingModels?.Any() == true)
                {
                    foreach (var sort in getPaymentClaimsModel.SortingModels)
                    {
                        result = result.AsQueryable()
                                       .OrderBy($"{sort.Field} {sort.Dir}")
                                       .ToList();
                    }
                }

                // Update calculated fields in result as before
                var payment = await _paymentRepository.GetByIdAsync(getPaymentClaimsModel.PaymentId);
                var groupedData = await GetGroupedByPayments(payment, GroupByParam.Claim, true);
                var patientClaimsIds = result.Select(r => r.Id).ToList();
                var patientClaims = await _paymentClaimRepository.Query().Where(x => patientClaimsIds.Contains(x.Id) && x.DateDeleted == null).ToListAsync();

                //Get Account Detail for Test Account
                var accountDetail = await _rethinkMasterDataMicroServices.GetAccountReturningEntityAsync(getPaymentClaimsModel.AccountInfoId, true);
                var accountType = accountDetail.AccountType;

                foreach (var element in result)
                {
                    var paymentClaimStatus = patientClaims.Where(x => x.Id == element.Id).Select(x => x.ClaimStatus).FirstOrDefault();
                    var notReversal = paymentClaimStatus != ((int)PaymentClaimStatus.Reversal).ToString();
                    var claimKey = element.ClaimId ?? 0;
                    var claimData = groupedData.FirstOrDefault(x => x.ClaimId == claimKey);

                    element.PaidAmount = notReversal ? (claimData?.InsurancePayment ?? 0) : 0;
                    element.PatientPayment = claimData?.PatientPayment ?? 0;
                    element.Adjustment = notReversal ? (claimData?.Adjustment ?? 0) : 0;
                    var totalWriteOffAmount = getClaimWriteOff(element.ClaimId ?? 0);
                    element.Adjustment -= totalWriteOffAmount;
                    element.PatientResponsibility = notReversal ? (claimData?.PatientResponsibility ?? 0) : 0;
                    element.Balance = (element.BilledAmount ?? 0) - (element.PaidAmount ?? 0) + (element.Adjustment ?? 0) + (element.PatientResponsibility ?? 0);
                    element.ExpectedAmount = element.BilledAmount;
                    element.isTestAccount = accountDetail.AccountType == 1;
                }

                // Apply additional filters after materialization if needed
                if (getPaymentClaimsModel.FilterModels.PaidAmountFrom.HasValue)
                    result = result.Where(x => x.PaidAmount >= getPaymentClaimsModel.FilterModels.PaidAmountFrom.Value).ToList();
                if (getPaymentClaimsModel.FilterModels.PaidAmountTo.HasValue)
                    result = result.Where(x => x.PaidAmount <= getPaymentClaimsModel.FilterModels.PaidAmountTo.Value).ToList();
                if (getPaymentClaimsModel.FilterModels.BalanceAmountFrom.HasValue)
                    result = result.Where(x => x.Balance >= getPaymentClaimsModel.FilterModels.BalanceAmountFrom.Value).ToList();
                if (getPaymentClaimsModel.FilterModels.BalanceAmountTo.HasValue)
                    result = result.Where(x => x.Balance <= getPaymentClaimsModel.FilterModels.BalanceAmountTo.Value).ToList();
                if (getPaymentClaimsModel.FilterModels.ShowPaid.HasValue)
                {
                    if (getPaymentClaimsModel.FilterModels.ShowPaid.Value)
                        result = result.Where(x => x.PaidAmount > 0).ToList();
                    else
                        result = result.Where(x => x.PaidAmount == 0).ToList();
                }

                var totalCount = result.Count;
                result = getPaymentClaimsModel.Take == 0
                    ? result.Skip(getPaymentClaimsModel.Skip).ToList()
                    : result.Skip(getPaymentClaimsModel.Skip).Take(getPaymentClaimsModel.Take).ToList();

                return new PaymentClaimsResponseModel
                {
                    Data = result,
                    TotalCount = totalCount,
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<PaymentPaitentModel>> GetPatientsByPaymentAsync(int paymentId)
        {
            var patients = await _paymentClaimRepository.Query()
                .Where(x => x.PaymentId == paymentId && x.DateDeleted == null)
                .Select(x => new PaymentPaitentModel
                {
                    PatientId = x.ChildProfileId,
                    PatientName = x.ClientFirstName + " " +
                                  (x.ClientMiddleName != null ? x.ClientMiddleName + " " : "") + x.ClientLastName
                })
                .Distinct()
                .ToListAsync();

            return patients;
        }

        private static string GetStatus(string? claimStatus)
        {
            if (int.TryParse(claimStatus, out int val))
            {
                if (val < 4)
                    return PaymentClaimStatus.Processed.ToString();

                return Enum.IsDefined(typeof(PaymentClaimStatus), val)
                    ? ((PaymentClaimStatus)val).ToString()
                    : PaymentClaimStatus.Unknown.ToString();
            }
            return PaymentClaimStatus.Unknown.ToString();
        }

        private static string GetClaimStatusDescription(ClaimStatus status)
        {
            var member = typeof(ClaimStatus).GetMember(status.ToString()).FirstOrDefault();
            if (member != null)
            {
                var descriptionAttr = member.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttr != null)
                    return descriptionAttr.Description;
            }
            return status.ToString();
        }


        /// <summary>
        /// used GetPaymentClaimsByPatientsAsyncNew tofetch patientdetails
        /// </summary>

        public async Task<PatientPaymentClaimsResponseModel> GetPaymentClaimsByPatientsAsync(GetClaimsModel getPaymentClaimsModel)
        {
            var payment = await _paymentRepository.Query()
                                                    .Where(x => x.Id == getPaymentClaimsModel.PaymentId && x.DateDeleted == null)
                                                    .Include(x => x.PaymentClaims.Where(pc => pc.DateDeleted == null))
                                                    .Include(x => x.UnallocatedPayments.Where(u => u.DateDeleted == null))
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync();


            var result = new List<PatientPaymentClaimFullModel>();

            var patientIds = payment.PaymentClaims.Select(x => x.ChildProfileId).Distinct().ToList();


            var latestUnallocByChild = (payment.UnallocatedPayments ?? Enumerable.Empty<UnAllocatedPaymentEntity>())
                                      .GroupBy(u => u.ChildProfileId)
                                      .ToDictionary(
                                          g => g.Key,
                                          g => g.OrderByDescending(x => x.DateCreated)
                                                .Select(x => (decimal?)x.UnAllocatedAmount)
                                                .FirstOrDefault() ?? 0m);

            var rows = new List<PatientPaymentClaimFullModel>(patientIds.Count);
            var totalPaymentClaimsByChild = payment.PaymentClaims
                                               .GroupBy(pc => pc.ChildProfileId)
                                               .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPayment));

            foreach (var item in patientIds)
            {
                var data = await GetPatientDetails(item, getPaymentClaimsModel.PaymentId, getPaymentClaimsModel.ShowPaid);
                if (data == null) continue;


                latestUnallocByChild.TryGetValue(item, out var latestUnalloc);
                totalPaymentClaimsByChild.TryGetValue(item, out var totalClaimed);

                data.UnallocatedPayment = (latestUnalloc) - (totalClaimed);

                result.Add(data);
            }

            var count = result.Count;

            result = result.AsQueryable().
                              OrderBy(getPaymentClaimsModel.SortingModels)
                             .Filter(getPaymentClaimsModel.FilterModels)
                             .Skip(getPaymentClaimsModel.Skip)
                             .Take(getPaymentClaimsModel.Take)
                             .ToList();

            var response = new PatientPaymentClaimsResponseModel()
            {
                Data = result,
                TotalCount = count
            };

            return response;

        }

        /// <summary>
        /// this is newly created method for payment posting where it calling GetPatientDetailsAsync for each patient
        /// </summary>

        public async Task<PatientPaymentClaimsResponseModel> GetPaymentClaimsByPatientsAsyncNew(GetClaimsModel getPaymentClaimsModel)
        {
            var payment = await _paymentRepository.Query()
                                                    .Where(x => x.Id == getPaymentClaimsModel.PaymentId && x.DateDeleted == null)
                                                    .Include(x => x.PaymentClaims.Where(pc => pc.DateDeleted == null))
                                                    .Include(x => x.UnallocatedPayments.Where(u => u.DateDeleted == null))
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync();


            var result = new List<PatientPaymentClaimFullModel>();
            var patientIds = payment.PaymentClaims
                               .Select(x => x.ChildProfileId)
                               .Concat(payment.UnallocatedPayments.Select(x => x.ChildProfileId))
                               .Distinct()
                               .ToList();

            var latestUnallocByChild = (payment.UnallocatedPayments ?? Enumerable.Empty<UnAllocatedPaymentEntity>())
                                      .GroupBy(u => u.ChildProfileId)
                                      .ToDictionary(
                                          g => g.Key,
                                          g => g.OrderByDescending(x => x.DateCreated)
                                                .Select(x => (decimal?)x.UnAllocatedAmount)
                                                .FirstOrDefault() ?? 0m);

            var rows = new List<PatientPaymentClaimFullModel>(patientIds.Count);
            var totalPaymentClaimsByChild = payment.PaymentClaims
                                               .GroupBy(pc => pc.ChildProfileId)
                                               .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPayment));

            TimeSpan? timeSpan = cacheExpiration;
            var clients = await _cacheService.GetOrSetCacheAsync(
               clientsCodeKey,
               async () => await _rethinkMasterDataMicroServices.GetChildProfilesForAccount(getPaymentClaimsModel.AccountInfoId),
               timeSpan.Value
            );

            foreach (var item in patientIds)
            {
                latestUnallocByChild.TryGetValue(item, out var latestUnalloc);
                totalPaymentClaimsByChild.TryGetValue(item, out var totalClaimed);
                var data = await GetPatientDetailsAsync(item, getPaymentClaimsModel.PaymentId, getPaymentClaimsModel.ShowPaid);
                if (data != null)
                {
                    data.UnallocatedPayment = (latestUnalloc) - (totalClaimed);
                }
                else
                {
                    // Defensive: find first claim for this patient, if any
                    var firstClaim = payment.PaymentClaims.FirstOrDefault(x => x.ChildProfileId == item);
                    var firstUnalloc = payment.UnallocatedPayments.FirstOrDefault(x => x.ChildProfileId == item);
                    var patientDetail = clients.FirstOrDefault(x => x.Id == item);

                    data = new PatientPaymentClaimFullModel
                    {
                        PaymentId = payment.Id,
                        PatientId = item,
                        ClaimId = firstClaim?.ClaimId ?? 0,
                        PatientName = patientDetail != null ? $"{patientDetail.FirstName} {patientDetail.MiddleName} {patientDetail.LastName}" : string.Empty,
                        InsurancePayment = 0,
                        PatientPayment = 0,
                        Adjustment = 0,
                        PatientResponsibility = 0,
                        PatientResponsibilityBalance = 0,
                        UnallocatedPayment = firstUnalloc?.UnAllocatedAmount ?? 0m
                    };
                }
                result.Add(data);
            }

            var count = result.Count;

            result = result.AsQueryable().
                              OrderBy(getPaymentClaimsModel.SortingModels)
                             .Filter(getPaymentClaimsModel.FilterModels)
                             .Skip(getPaymentClaimsModel.Skip)
                             .Take(getPaymentClaimsModel.Take)
                             .ToList();

            var response = new PatientPaymentClaimsResponseModel()
            {
                Data = result,
                TotalCount = count
            };

            return response;

        }

        [Obsolete("Use GetPatientDetailsAsync instead")]
        private async Task<PatientPaymentClaimFullModel> GetPatientDetails(int patientId, int paymentId, bool showPaid)
        {
            var linkedItes = await GetPatientPaymentLinkedServiceLinesAsync(new GetPatientPaymentServiceLinesModel
            {
                PatientId = patientId,
                PaymentId = paymentId,
                ShowPaid = showPaid,
                IsLinked = true,
                SortingModels = new List<SortingModel>()
            }, true);
            var unLinkedItes = await GetPatientPaymentUnlinkedServiceLinesAsync(new GetPatientPaymentServiceLinesModel
            {
                PatientId = patientId,
                PaymentId = paymentId,
                ShowPaid = showPaid,
                IsLinked = false,
                SortingModels = new List<SortingModel>()
            });

            if (linkedItes.Count == 0 && unLinkedItes.Count == 0) return null;

            return new PatientPaymentClaimFullModel
            {
                PaymentId = paymentId,
                PatientId = linkedItes.Any() ? linkedItes.First().PatientId : unLinkedItes.First().PatientId,
                PatientName = linkedItes.Any() ? linkedItes.First().PatientName : unLinkedItes.First().PatientName,
                InsurancePayment = linkedItes.Sum(x => x.InsurancePayment) + unLinkedItes.Sum(x => x.InsurancePayment),
                PatientPayment = linkedItes.Sum(x => x.PatientPayment) + unLinkedItes.Sum(x => x.PatientPayment),
                Adjustment = linkedItes.Sum(x => x.Adjustment) + unLinkedItes.Sum(x => x.Adjustment),
                PatientResponsibility = linkedItes.Sum(x => x.PatientResponsibility) + unLinkedItes.Sum(x => x.PatientResponsibility),
                PatientResponsibilityBalance = linkedItes.Sum(x => x.PatientResponsibilityBalance) + unLinkedItes.Sum(x => x.PatientResponsibilityBalance)
            };
        }

        private async Task<PatientPaymentClaimFullModel> GetPatientDetailsAsync(int patientId, int paymentId, bool showPaid)
        {
            var linkedItes = await GetPatientPaymentLinkedServiceLinesAsyncNew(new GetPatientPaymentServiceLinesModel
            {
                PatientId = patientId,
                PaymentId = paymentId,
                ShowPaid = showPaid,
                IsLinked = true,
                SortingModels = new List<SortingModel>()
            }, true);
            var unLinkedItes = await GetPatientPaymentUnlinkedServiceLinesAsyncNew(new GetPatientPaymentServiceLinesModel
            {
                PatientId = patientId,
                PaymentId = paymentId,
                ShowPaid = showPaid,
                IsLinked = false,
                SortingModels = new List<SortingModel>()
            });

            if (linkedItes.Count == 0 && unLinkedItes.Count == 0) return null;

            return new PatientPaymentClaimFullModel
            {
                PaymentId = paymentId,
                PatientId = linkedItes.Any() ? linkedItes.First().PatientId : unLinkedItes.First().PatientId,
                PatientName = linkedItes.Any() ? linkedItes.First().PatientName : unLinkedItes.First().PatientName,
                InsurancePayment = linkedItes.Sum(x => x.InsurancePayment) + unLinkedItes.Sum(x => x.InsurancePayment),
                PatientPayment = linkedItes.Sum(x => x.PatientPayment) + unLinkedItes.Sum(x => x.PatientPayment),
                Adjustment = linkedItes.Sum(x => x.Adjustment) + unLinkedItes.Sum(x => x.Adjustment),
                PatientResponsibility = linkedItes.Sum(x => x.PatientResponsibility) + unLinkedItes.Sum(x => x.PatientResponsibility),
                PatientResponsibilityBalance = linkedItes.Sum(x => x.PatientResponsibilityBalance) + unLinkedItes.Sum(x => x.PatientResponsibilityBalance)
            };
        }

        public async Task<List<PaymentClaimServiceLineModel>> GetPaymentClaimServiceLinesAsync(int claimId)
        {
            var payment = await _paymentClaimRepository.Query().Where(x => x.Id == claimId).Select(x => x.Payment).FirstOrDefaultAsync();

            var paymentClaimStatus = await _paymentClaimRepository.Query()
                        .Where(x => x.Id == claimId && x.DateDeleted == null).Select(x => x.ClaimStatus).FirstOrDefaultAsync();

            var groupedData = await GetGroupedByPayments(payment, GroupByParam.Charge, true);

            var filteredData = _allChargeData.Where(x => x.PaymentClaimId == claimId && x.PaymentId == payment.Id && x.DateDeleted == null).ToList();

            var result = filteredData.Select(x => new PaymentClaimServiceLineModel
            {
                Id = x.ServiceLineId,
                PatientId = x.PatientId,
                ChargeEntryId = x.ChargeId,
                DateOfService = x.DateOfService,
                Procedure = x.ServiceCode,
                AllowedAmount = x.AllowedAmount ?? 0,
                BilledAmount = x.ChargeAmount ?? 0,
                PaidAmount = x.PaidAmount ?? 0,
                Adjustment = 0,
                PatientResponsibility = 0,
                Balance = 0,
                Mods = string.Join(",", x.ProcedureModifier1, x.ProcedureModifier2, x.ProcedureModifier3,
                        x.ProcedureModifier4),
                DateLastModified = x.DateLastModified,
                ClaimId = x.PaymentClaimId,
                HasErrors = x.HasErrors
            }).ToList();

            foreach (var data in result)
            {
                var paymentInfo = groupedData.Where(x => x.ChargeId == data.ChargeEntryId).FirstOrDefault();

                data.PatientResponsibility = paymentClaimStatus != ((int)PaymentClaimStatus.Reversal).ToString() ? paymentInfo.PatientResponsibility : 0;
                data.Adjustment = paymentClaimStatus != ((int)PaymentClaimStatus.Reversal).ToString() ? paymentInfo.Adjustment : 0;
                data.Adjustment -= getChargeEntryWriteOff(data.ChargeEntryId ?? 0);
                data.InsurancePayment = paymentClaimStatus != ((int)PaymentClaimStatus.Reversal).ToString() ? paymentInfo.InsurancePayment : 0;
                data.PaidAmount = paymentClaimStatus != ((int)PaymentClaimStatus.Reversal).ToString() ? paymentInfo.InsurancePayment : 0;
                data.PatientPayment = paymentInfo.PatientPayment;
                var modifiers = await _claimChargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == data.ChargeEntryId);
                if (modifiers != null)
                {
                    data.Mods = string.Join(",", modifiers.Modifier1, modifiers.Modifier2, modifiers.Modifier3, modifiers.Modifier4);
                    data.Mods = string.Join(", ", data.Mods.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
                }
                else
                {
                    data.Mods = string.Empty;
                }
                data.Balance = data.BilledAmount - data.InsurancePayment + data.Adjustment + data.PatientResponsibility;
                data.ExpectedAmount = data.BilledAmount;
                data.PatientResponsibilityBalance = paymentInfo.PatientResponsibilityBalance;
            }

            return result.OrderByDescending(x => x.DateOfService).ToList();
        }

        [Obsolete("Use GetPatientPaymentLinkedServiceLinesAsyncNew instead")]
        public async Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentLinkedServiceLinesAsync(GetPatientPaymentServiceLinesModel model, bool isPatientDetailsLoading = false)
        {
            if (model.SortingModels.Count == 0)
            {
                model.SortingModels.Add(new SortingModel { Field = "patientPayment", Dir = "desc" });
            }

            var payment = await _paymentRepository.GetByIdAsync(model.PaymentId);

            var groupedData = await GetGroupedByPayments(payment, GroupByParam.Charge, true);

            var filteredData = _allChargeData.Where(x => x.PatientId == model.PatientId && x.PaymentId == model.PaymentId).ToList();

            var result = filteredData.Select(x => new PaymentClaimServiceLineModel
            {
                Id = x.ServiceLineId,
                PatientId = x.PatientId,
                PatientName = x.PatientName,
                ChargeEntryId = x.ChargeId,
                DateOfService = x.DateOfService,
                Procedure = x.ServiceCode,
                AllowedAmount = x.AllowedAmount ?? 0,
                BilledAmount = x.ChargeAmount ?? 0,
                PaidAmount = x.PaidAmount ?? 0,
                Adjustment = 0,
                PatientResponsibility = 0,
                Balance = 0,
                Mods = string.Join(",", x.ProcedureModifier1, x.ProcedureModifier2, x.ProcedureModifier3,
                        x.ProcedureModifier4),
                DateLastModified = x.DateLastModified,
                ClaimId = x.PaymentClaimId,
                HasErrors = x.HasErrors,
                IsLinked = (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) && x.PaidAmount > 0
            }).ToList();

            var dataForNoResult = result.First();
            result = result.Where(x => x.IsLinked == true).ToList();

            if (result.Count == 0 && isPatientDetailsLoading)
            {
                dataForNoResult.PatientResponsibility = 0;
                dataForNoResult.Adjustment = 0;
                dataForNoResult.InsurancePayment = 0;
                dataForNoResult.PatientPayment = 0;
                dataForNoResult.Balance = 0;
                dataForNoResult.ExpectedAmount = 0;
                dataForNoResult.PatientResponsibilityBalance = 0;
                result.Add(dataForNoResult);
            }
            else
            {
                foreach (var data in result)
                {
                    var paymentInfo = groupedData.Where(x => x.ChargeId == data.ChargeEntryId).FirstOrDefault();

                    data.PatientResponsibility = paymentInfo.PatientResponsibility;
                    data.Adjustment = paymentInfo.Adjustment;
                    data.Adjustment += getChargeEntryWriteOff(data.ChargeEntryId ?? 0);
                    data.InsurancePayment = paymentInfo.InsurancePayment;
                    data.PatientPayment = paymentInfo.PatientPayment;
                    var modifiers = await _claimChargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == data.ChargeEntryId);
                    data.Mods = string.Join(",", modifiers.Modifier1, modifiers.Modifier2, modifiers.Modifier3, modifiers.Modifier4);
                    data.Mods = string.Join(", ", data.Mods.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
                    data.Balance = data.BilledAmount - data.InsurancePayment - data.Adjustment - data.PatientResponsibility;
                    data.ExpectedAmount = data.BilledAmount;
                    data.PatientResponsibilityBalance = paymentInfo.PatientResponsibilityBalance;
                }
            }
            result = result.AsQueryable()
            .OrderBy(model.SortingModels)
            .Filter(model.FilterModels)
            .ToList();

            return result;
        }

        [Obsolete("Use GetPatientPaymentUnlinkedServiceLinesAsyncNew instead")]
        public async Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentUnlinkedServiceLinesAsync(GetPatientPaymentServiceLinesModel model)
        {
            if (model.SortingModels.Count == 0)
            {
                model.SortingModels.Add(new SortingModel { Field = "patientPayment", Dir = "desc" });
            }

            var payment = await _paymentRepository.GetByIdAsync(model.PaymentId);

            var groupedData = await GetGroupedByPayments(payment, GroupByParam.Charge);

            var filteredData = _allChargeData.Where(x => x.PatientId == model.PatientId &&
                                                     x.PaymentId == model.PaymentId &&
                                                     x.DateOfService <= payment.DepositDate).ToList();

            var result = filteredData.Select(x => new PaymentClaimServiceLineModel
            {
                Id = x.ServiceLineId,
                PatientId = x.PatientId,
                PatientName = x.PatientName,
                ChargeEntryId = x.ChargeId,
                DateOfService = x.DateOfService,
                Procedure = x.ServiceCode,
                AllowedAmount = x.AllowedAmount ?? 0,
                BilledAmount = x.ChargeAmount ?? 0,
                PaidAmount = x.PaidAmount ?? 0,
                Adjustment = 0,
                PatientResponsibility = 0,
                Balance = 0,
                Mods = string.Join(",", x.ProcedureModifier1, x.ProcedureModifier2, x.ProcedureModifier3,
                        x.ProcedureModifier4),
                DateLastModified = x.DateLastModified,
                ClaimId = x.PaymentClaimId,
                HasErrors = x.HasErrors,
                IsLinked = (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) && x.PaidAmount > 0
            }).ToList();

            result = result.Where(x => !x.IsLinked == true).ToList();

            foreach (var data in result)
            {
                var paymentInfo = groupedData.Where(x => x.ChargeId == data.ChargeEntryId).FirstOrDefault();

                data.PatientResponsibility = paymentInfo.PatientResponsibility;
                data.Adjustment = paymentInfo.Adjustment;
                data.Adjustment += getChargeEntryWriteOff(data.ChargeEntryId ?? 0);
                data.InsurancePayment = paymentInfo.InsurancePayment;
                data.PatientPayment = paymentInfo.PatientPayment;
                var modifiers = await _claimChargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == data.ChargeEntryId);
                data.Mods = string.Join(",", modifiers.Modifier1, modifiers.Modifier2, modifiers.Modifier3, modifiers.Modifier4);
                data.Mods = string.Join(", ", data.Mods.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
                data.Balance = data.BilledAmount - data.InsurancePayment - data.Adjustment - data.PatientResponsibility;
                data.ExpectedAmount = data.BilledAmount;
                data.PatientResponsibilityBalance = paymentInfo.PatientResponsibilityBalance;
            }
            result = model.ShowPaid ? result.Where(x => x.PatientResponsibility != 0 && x.PatientPayment > 0).ToList() : result.Where(x => x.PatientResponsibility != 0).ToList();

            result = result.AsQueryable()
            .OrderBy(model.SortingModels)
            .Filter(model.FilterModels)
            .ToList();

            return result;
        }


        public async Task<PaymentClaimServiceLineModel> GetPaymentClaimServiceLineAsync(int serviceLineId)
        {
            var result = await _paymentClaimServiceLineRepository.Query()
            .Where(x => x.Id == serviceLineId)
            .Select(x => new PaymentClaimServiceLineModel
            {
                Id = x.Id,

                PatientId = x.PaymentClaim.ChildProfileId,
                PaymentId = x.PaymentClaim.PaymentId,
                PaymentTypeId = x.PaymentClaim.Payment.PaymentTypeId,
                ChargeEntryId = x.ClaimChargeEntryId,
                DateOfService = x.DateOfService,
                Procedure = x.ServiceCode,
                AllowedAmount = x.AllowedAmount ?? 0,
                BilledAmount = x.ChargeAmount ?? 0,
                PaidAmount = x.PaymentAmount ?? 0,
                ServiceLinePaymentAmount = x.PaymentAmount ?? 0,
                Mods = string.Join(",", x.ProcedureModifier1, x.ProcedureModifier2, x.ProcedureModifier3,
                        x.ProcedureModifier4),
                DateLastModified = x.DateLastModified,
                ClaimId = x.PaymentClaimId ?? 0,
                ClaimIdentifier = x.PaymentClaim.ClaimIdentifier,
                HasErrors = x.PaymentClaimServiceLineErrors.Any() || x.PaymentClaim.PaymentClaimErrors.Any()
            }).FirstOrDefaultAsync();

            var payment = await _paymentRepository.GetByIdAsync(result.PaymentId);


            var modifiers = await _claimChargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == result.ChargeEntryId);
            if (modifiers != null)
            {
                result.Mods = string.Join(",", modifiers.Modifier1, modifiers.Modifier2, modifiers.Modifier3, modifiers.Modifier4);
                result.Mods = string.Join(", ", result.Mods.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            else
            {
                result.Mods = string.Empty;
            }

            if (result.PaymentTypeId == (int)PaymentTypes.ClientPayment || result.PaymentTypeId == (int)PaymentTypes.OtherPayment)
            {
                var groupByData = await GetGroupedByPayments(payment, GroupByParam.Charge, result.PaidAmount > 0);
                var serviceLineData = groupByData.Where(x => x.ChargeId == result.ChargeEntryId).FirstOrDefault();
                result.Adjustment = serviceLineData.Adjustment;
                result.PatientResponsibility = serviceLineData.PatientResponsibility;
                result.InsurancePayment = serviceLineData.InsurancePayment;
                result.PatientPayment = serviceLineData.PatientPayment;
                //result.ServiceLinePaymentAmount = result.PatientPayment;
                result.Adjustment += getChargeEntryWriteOff(result.ChargeEntryId ?? 0);
                result.Balance = result.BilledAmount - result.InsurancePayment - result.Adjustment - result.PatientResponsibility;
                result.PatientResponsibilityBalance = result.PatientResponsibility - result.PatientPayment;
            }
            else
            {
                var groupByData = await GetGroupedByPayments(payment, GroupByParam.Charge, true);
                var serviceLineData = groupByData.Where(x => x.ChargeId == result.ChargeEntryId).FirstOrDefault();
                result.Adjustment = serviceLineData?.Adjustment;
                result.PatientResponsibility = serviceLineData?.PatientResponsibility;
                result.InsurancePayment = serviceLineData?.InsurancePayment;
                result.PatientPayment = serviceLineData?.PatientPayment;
                //result.ServiceLinePaymentAmount = result.InsurancePayment;
                result.Adjustment -= getChargeEntryWriteOff(result.ChargeEntryId ?? 0);
                result.Balance = result.BilledAmount - result.InsurancePayment + result.Adjustment + result.PatientResponsibility;
            }

            var unAllocatedPmt = _unAllocatedPaymentRepository.Query().AsNoTracking()
                .Where(x => x.ChildProfileId == result.PatientId && x.PaymentId == result.PaymentId && x.DateDeleted == null)
                .OrderByDescending(x => x.DateCreated).FirstOrDefault();

            result.UnallocatedPayment = unAllocatedPmt?.UnAllocatedAmount ?? 0 - Convert.ToDecimal(result.ServiceLinePaymentAmount ?? 0);
            result.ExpectedAmount = result.BilledAmount;
            return result;
        }

        public async Task<List<PaymentGroupedModel>> GetChargeInfoByIds(List<int> chargeIds)
        {
            _logger.LogInformation("GetChargeInfoByIds Started chargeIds={chargeIds}", chargeIds.Count);
            var result = await _paymentClaimServiceLineRepository
                           .Query()
                           .AsNoTracking()
                           .Where(sl =>
                               sl.ClaimChargeEntryId.HasValue &&
                               chargeIds.Contains(sl.ClaimChargeEntryId.Value) &&
                               sl.DateDeleted == null &&
                               sl.PaymentClaim.DateDeleted == null &&
                               (
                                   sl.PaymentClaim.Claim.DateDeleted == null ||
                                   sl.PaymentClaim.Claim.isPrivatePayClaim == true
                               ))
                           .Select(sl => new PaymentGroupedModel
                           {
                               PaymentId = sl.PaymentClaim.PaymentId,
                               PaymentTypeId = sl.PaymentClaim.Payment.PaymentTypeId,
                               PaymentClaimId = sl.PaymentClaimId ?? 0,
                               ChargeId = sl.ClaimChargeEntryId ?? 0,
                               ClaimId = sl.PaymentClaim.ClaimId ?? 0,
                               PatientId = sl.PaymentClaim.ChildProfileId,
                               PatientName =
                                   sl.PaymentClaim.ClientFirstName + " " +
                                   sl.PaymentClaim.ClientMiddleName + " " +
                                   sl.PaymentClaim.ClientLastName,

                               ServiceLineId = sl.Id,
                               ChargeAmount = sl.ChargeAmount,
                               AllowedAmount = sl.AllowedAmount,
                               PaidAmount = sl.PaymentAmount,
                               DateOfService = sl.DateOfService,
                               ServiceCode = sl.ServiceCode,
                               ProcedureModifier1 = sl.ProcedureModifier1,
                               ProcedureModifier2 = sl.ProcedureModifier2,
                               ProcedureModifier3 = sl.ProcedureModifier3,
                               ProcedureModifier4 = sl.ProcedureModifier4,
                               DateLastModified = sl.DateLastModified,
                               DateDeleted = sl.DateDeleted,
                               // EXISTS instead of Include + Any
                               HasErrors =
                                   sl.PaymentClaimServiceLineErrors.Any(e => e.DateDeleted == null) ||
                                   sl.PaymentClaim.PaymentClaimErrors.Any(e => e.DateDeleted == null),

                               // Filtered child collection (no Include)
                               Adjustments =
                                   sl.PaymentClaimServiceLineAdjustments
                                       .Where(a => a.DateDeleted == null)
                                       .ToList()
                           })
                           .ToListAsync();
            _logger.LogInformation("GetChargeInfoByIds Started result={result}", result.Count);

            return result;
        }
        public async Task<List<PaymentGroupedModel>> GetAllCharges(int paymentId)
        {
            await GetAllCharges(paymentId, 0);
            return _allChargeData;
        }
        public async Task GetAllCharges(int paymentId, int childProfileId)
        {
            var claimIds = new List<int>();
            if (childProfileId == 0)
            {
                claimIds = await _paymentClaimRepository.Query().Where(x => x.PaymentId == paymentId && x.DateDeleted == null).Select(x => x.ClaimId ?? 0).ToListAsync();
            }
            else
            {
                claimIds = await _claimEntityRepository.Query().Where(x => x.ChildProfileId == childProfileId && (x.DateDeleted == null || x.isPrivatePayClaim == true)).Select(x => x.Id).ToListAsync();
            }

            var details = await _paymentClaimServiceLineRepository.Query()
                            .Include(x => x.PaymentClaim).ThenInclude(x => x.Payment)
                            .Include(x => x.PaymentClaimServiceLineAdjustments)
                            .Include(x => x.PaymentClaimServiceLineErrors)
                            .Where(x => claimIds.Contains(x.PaymentClaim.ClaimId ?? 0) &&
                                        x.DateDeleted == null &&
                                        x.PaymentClaim.DateDeleted == null &&
                                        (x.PaymentClaim.Claim.DateDeleted == null || x.PaymentClaim.Claim.isPrivatePayClaim == true))
                            .ToListAsync();
            _allChargeData = arrangeInfo(details);

            _paymentId = paymentId;
        }

        private List<PaymentGroupedModel> arrangeInfo(List<PaymentClaimServiceLineEntity> data)
        {
            var details = data.Select(x => new PaymentGroupedModel
            {
                PaymentId = x.PaymentClaim.PaymentId,
                PaymentTypeId = x.PaymentClaim.Payment.PaymentTypeId,
                PaymentClaimId = x.PaymentClaimId ?? 0,
                ChargeId = x.ClaimChargeEntryId ?? 0,
                ClaimId = x.PaymentClaim.ClaimId ?? 0,
                PatientId = x.PaymentClaim.ChildProfileId,
                PatientName = $"{x.PaymentClaim.ClientFirstName} {x.PaymentClaim.ClientMiddleName} {x.PaymentClaim.ClientLastName}",
                ServiceLineId = x.Id,
                ChargeAmount = x.ChargeAmount,
                AllowedAmount = x.AllowedAmount,
                PaidAmount = x.PaymentAmount,
                DateOfService = x.DateOfService,
                ServiceCode = x.ServiceCode,
                ProcedureModifier1 = x.ProcedureModifier1,
                ProcedureModifier2 = x.ProcedureModifier2,
                ProcedureModifier3 = x.ProcedureModifier3,
                ProcedureModifier4 = x.ProcedureModifier4,
                DateLastModified = x.DateLastModified,
                DateDeleted = x.DateDeleted,
                Adjustments = x.PaymentClaimServiceLineAdjustments.Where(x => x.DateDeleted == null).ToList(),
                HasErrors = x.PaymentClaimServiceLineErrors.Any() || x.PaymentClaim.PaymentClaimErrors.Any()
            }).ToList();

            return details;
        }

        private async Task<List<PatientPaymentClaimFullModel>> GroupByCharges(List<PaymentGroupedModel> charges, GroupByParam groupBy, bool isRequiredForInvoicing, PaymentEntity payment = null)
        {
            try
            {
                var response = new List<PatientPaymentClaimFullModel>();

                if (groupBy == GroupByParam.Patient) // group by patient
                {
                    response = charges.GroupBy(pr => new { pr.PatientId, pr.PatientName }).Select(x => new PatientPaymentClaimFullModel
                    {
                        PaymentId = payment.Id,
                        PatientId = x.Key.PatientId,
                        PatientName = x.Key.PatientName,
                        totalAmount = x.Where(y => y.PatientId == x.Key.PatientId).Sum(x => x.ChargeAmount),
                        InsurancePayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.InsurancePayment || x.PaymentTypeId == (int)PaymentTypes.ERAReceived) && x.DateDeleted == null).Sum(x => x.PaidAmount),

                        PatientPayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) && x.DateDeleted == null).Sum(x => x.PaidAmount),

                        PositiveAdjustment = x.Sum(x => x.Adjustments
                                                            .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode != "PR"
                                                                && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        NegativeAdjustment = x.Sum(x => x.Adjustments
                                                            .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                                && x.AdjustmentGroupCode != "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        PositivePatientResponsibility = x.Sum(x => x.Adjustments
                                                                    .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode == "PR"
                                                                        && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        NegativePatientResponsibility = x.Sum(x => x.Adjustments
                                                                    .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                                        && x.AdjustmentGroupCode == "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount))
                    }).ToList();
                }
                else if (groupBy == GroupByParam.Claim) // group by claim
                {
                    response = charges.GroupBy(pr => new { pr.ClaimId }).Select(x => new PatientPaymentClaimFullModel
                    {
                        ClaimId = x.Key.ClaimId,
                        totalAmount = x.Where(x => x.PaymentClaimId == x.PaymentClaimId).Sum(x => x.ChargeAmount),
                        InsurancePayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.InsurancePayment || x.PaymentTypeId == (int)PaymentTypes.ERAReceived) &&
                                                            x.DateDeleted == null).Sum(x => x.PaidAmount),

                        PatientPayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) &&
                                                            x.DateDeleted == null).Sum(x => x.PaidAmount),

                        PositiveAdjustment = x.Sum(x => x.Adjustments
                                                            .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode != "PR"
                                                                && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        NegativeAdjustment = x.Sum(x => x.Adjustments
                                                            .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                                && x.AdjustmentGroupCode != "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        PositivePatientResponsibility = x.Sum(x => x.Adjustments
                                                                    .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode == "PR"
                                                                        && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        NegativePatientResponsibility = x.Sum(x => x.Adjustments
                                                                    .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                                        && x.AdjustmentGroupCode == "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),
                    }).ToList();


                }
                else if (groupBy == GroupByParam.Charge) // group by charge
                {
                    response = charges.GroupBy(pr => new { pr.ChargeId }).Select(x => new PatientPaymentClaimFullModel
                    {
                        ChargeId = x.Key.ChargeId,
                        ClaimId = x.First().ClaimId,
                        PatientId = x.First().PatientId,
                        PatientName = x.First().PatientName,
                        ServiceCode = x.First().ServiceCode,
                        DateOfService = (DateTime)x.First().DateOfService,
                        totalAmount = x.First().ChargeAmount,
                        InsurancePayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.InsurancePayment || x.PaymentTypeId == (int)PaymentTypes.ERAReceived) &&
                                                            x.DateDeleted == null).Sum(x => x.PaidAmount),

                        PatientPayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) &&
                                                            x.DateDeleted == null).Sum(x => x.PaidAmount),

                        PositiveAdjustment = x.Sum(x => x.Adjustments
                                                            .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode != "PR"
                                                                && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        NegativeAdjustment = x.Sum(x => x.Adjustments
                                                            .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                                && x.AdjustmentGroupCode != "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        PositivePatientResponsibility = x.Sum(x => x.Adjustments
                                                                    .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode == "PR"
                                                                        && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                        NegativePatientResponsibility = x.Sum(x => x.Adjustments
                                                                    .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                                        && x.AdjustmentGroupCode == "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),
                    }).ToList();
                }

                response.ForEach(x =>
                {
                    if (isRequiredForInvoicing || (payment != null && (payment.PaymentTypeId == (int)PaymentTypes.ClientPayment || payment.PaymentTypeId == (int)PaymentTypes.OtherPayment)))
                    {
                        x.Adjustment = x.NegativeAdjustment - x.PositiveAdjustment;
                        x.PatientResponsibility = x.NegativePatientResponsibility - x.PositivePatientResponsibility;
                    }
                    else
                    {
                        x.Adjustment = x.PositiveAdjustment - x.NegativeAdjustment;
                        x.PatientResponsibility = x.PositivePatientResponsibility - x.NegativePatientResponsibility;
                    }
                    x.PatientResponsibilityBalance = x.PatientResponsibility - x.PatientPayment;
                });

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // this method will used for if want to fetch service linked/unlinked charges for Patient tyep payment posting as used Adjustment as DTO class
        // instead of entity to pass on to controller(application layer)
        private async Task<List<PatientPaymentClaimFullModel>> GroupByChargesForPatient(List<PaymentGroupedModel> charges, GroupByParam groupBy, bool isRequiredForInvoicing, PaymentEntity payment = null)
        {
            var response = new List<PatientPaymentClaimFullModel>();

            if (groupBy == GroupByParam.Charge) // group by charge
            {
                response = charges.GroupBy(pr => new { pr.ChargeId }).Select(x => new PatientPaymentClaimFullModel
                {
                    ChargeId = x.Key.ChargeId,
                    ClaimId = x.First().ClaimId,
                    PatientId = x.First().PatientId,
                    PatientName = x.First().PatientName,
                    ServiceCode = x.First().ServiceCode,
                    DateOfService = (DateTime)x.First().DateOfService,
                    totalAmount = x.First().ChargeAmount,
                    InsurancePayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.InsurancePayment || x.PaymentTypeId == (int)PaymentTypes.ERAReceived) &&
                                                        x.DateDeleted == null).Sum(x => x.PaidAmount),

                    PatientPayment = x.Where(x => (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) &&
                                                        x.DateDeleted == null).Sum(x => x.PaidAmount),

                    PositiveAdjustment = x.Sum(x => x.Adjustment
                                                        .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode != "PR"
                                                            && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                    NegativeAdjustment = x.Sum(x => x.Adjustment
                                                        .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                            && x.AdjustmentGroupCode != "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                    PositivePatientResponsibility = x.Sum(x => x.Adjustment
                                                                .Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode == "PR"
                                                                    && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),

                    NegativePatientResponsibility = x.Sum(x => x.Adjustment
                                                                .Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                                    && x.AdjustmentGroupCode == "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount)),
                }).ToList();
            }

            response.ForEach(x =>
            {
                if (isRequiredForInvoicing || (payment != null && (payment.PaymentTypeId == (int)PaymentTypes.ClientPayment || payment.PaymentTypeId == (int)PaymentTypes.OtherPayment)))
                {
                    x.Adjustment = x.NegativeAdjustment - x.PositiveAdjustment;
                    x.PatientResponsibility = x.NegativePatientResponsibility - x.PositivePatientResponsibility;
                }
                else
                {
                    x.Adjustment = x.PositiveAdjustment - x.NegativeAdjustment;
                    x.PatientResponsibility = x.PositivePatientResponsibility - x.NegativePatientResponsibility;
                }
                x.PatientResponsibilityBalance = x.PatientResponsibility - x.PatientPayment;
            });

            return response;
        }

        public async Task<List<BasicChargeDetails>> GetAllPaymentChargeIds(CreateInvoiceFilters model)
        {
            var query = _paymentClaimServiceLineRepository
                .Query()
                .AsNoTracking()
                .Where(x =>
                    x.DateDeleted == null &&
                    x.PaymentClaim.DateDeleted == null &&
                    x.PaymentClaim.Claim.AccountInfoId == model.AccountInfoId
                );

            // ---- Client filter
            if (!string.IsNullOrWhiteSpace(model.Filters.ClientIds))
            {
                var clientIds = model.Filters.ClientIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();

                query = query.Where(x => clientIds.Contains(x.PaymentClaim.ChildProfileId));
            }

            // ---- DateOfService From
            if (model.Filters.DateOfServiceFrom.HasValue)
            {
                var fromDate = ConvertToCompleteDate(model.Filters.DateOfServiceFrom.Value);
                query = query.Where(x => x.DateOfService >= fromDate);
            }

            // ---- DateOfService To
            if (model.Filters.DateOfServiceTo.HasValue)
            {
                var toDate = ConvertToCompleteDate(model.Filters.DateOfServiceTo.Value);
                query = query.Where(x => x.DateOfService <= toDate);
            }

            return await query
                .Select(x => new BasicChargeDetails
                {
                    ClientId = x.PaymentClaim.ChildProfileId,
                    DateOfService = x.DateOfService,
                    ChargeId = x.ClaimChargeEntryId
                })
                .Distinct()
                .ToListAsync();
        }


        public async Task<List<PaymentGroupedModel>> GetAllChargesCachedAsync(int paymentId, int patientId, int childProfileId, TimeSpan? timeSpan = null)
        {
            timeSpan ??= cacheExpiration;
            var key = AllChargesKey(paymentId, patientId);

            return await _cacheService.GetOrSetCacheAsync(
                              key,
                              async () =>
                              {
                                  await GetAllChargesNew(paymentId, patientId, childProfileId);
                                  return _allChargeDataCached ?? new List<PaymentGroupedModel>();
                              },
                              timeSpan.Value
                          );
        }

        public async Task<Dictionary<int, PatientPaymentClaimFullModel>> GetGroupedDictCachedAsync(PaymentEntity payment, int patientId, int childProfileId, bool isLinked, GroupByParam groupBy, TimeSpan? timeSpan = null)
        {
            if (payment == null) return new Dictionary<int, PatientPaymentClaimFullModel>();
            timeSpan ??= cacheExpiration;
            var key = GroupedKey(payment.Id, patientId, isLinked);

            return await _cacheService.GetOrSetCacheAsync(key, async () =>
            {
                // Ensure all charges are cached (this will call GetAllChargesNew once per cache miss)
                var all = await GetAllChargesCachedAsync(payment.Id, patientId, childProfileId, timeSpan);

                // Use GroupByCharges to produce authoritative aggregated results (you already have business rules there)
                var groupedList = await GroupByChargesForPatient(all, groupBy, false, payment);

                // Build dictionary keyed by ChargeId; defend against duplicates by picking First()
                var dict = groupedList
                 .Where(x => x.ChargeId != 0)
                 .GroupBy(x => x.ChargeId)
                 .Select(g => g.First())
                 .ToDictionary(x => x.ChargeId, x => x);

                return dict;
            }, timeSpan.Value
            );
        }

        public async Task GetAllChargesNew(int paymentId, int patientId, int childProfileId)
        {
            var cacheKey = GetCacheKey(paymentId, patientId);

            // 1) Try to get from cache for this (paymentId, childProfileId)
            //if (_allChargeDataCacheDictionary.TryGetValue(cacheKey, out var cachedList))
            //{
            //    _allChargeDataCached = cachedList;
            //    return;
            //}

            List<int> claimIds;
            if (childProfileId == 0)
            {
                claimIds = await _paymentClaimRepository.Query()
                    .Where(x => x.PaymentId == paymentId && x.DateDeleted == null)
                    .Select(x => x.ClaimId ?? 0)
                    .ToListAsync();
            }
            else
            {
                claimIds = await _claimEntityRepository.Query()
                    .Where(x => x.ChildProfileId == childProfileId && (x.DateDeleted == null || x.isPrivatePayClaim == true))
                    .Select(x => x.Id)
                    .ToListAsync();
            }


            var claimIdSet = claimIds.ToHashSet();

            // 2) load minimal service-line data
            var serviceLines = await _paymentClaimServiceLineRepository.Query()
                .Where(x => claimIdSet.Contains(x.PaymentClaim.ClaimId ?? 0)
                            && x.DateDeleted == null
                            && x.PaymentClaim.DateDeleted == null
                            && (x.PaymentClaim.Claim.DateDeleted == null || x.PaymentClaim.Claim.isPrivatePayClaim == true))
                .AsNoTracking()
                .Select(s => new
                {
                    ServiceLineId = s.Id,
                    s.PaymentClaimId,
                    s.ServiceCode,
                    s.ClaimChargeEntryId,
                    s.PaymentClaim.ClaimId,
                    s.DateOfService,
                    s.AllowedAmount,
                    s.PaymentClaim.PaymentId,
                    s.PaymentClaim.Payment.PaymentTypeId,
                    s.PaymentClaim.Claim.ChildProfileId,
                    s.PaymentClaim.PatientId,
                    PatientName = (s.PaymentClaim.ClientFirstName ?? "") + " " + (s.PaymentClaim.ClientMiddleName ?? "") + " " + (s.PaymentClaim.ClientLastName ?? ""),
                    s.ChargeAmount,
                    PaidAmount = s.PaymentAmount ?? 0m,
                    s.DateLastModified,
                    s.DateDeleted
                })
                .ToListAsync();

            var serviceLineIds = serviceLines.Select(l => l.ServiceLineId).ToHashSet();

            // 3) load adjustments for these lines
            var adjustments = await _paymentClaimServiceLineAdjustmentRepository.Query()
                .Where(a => serviceLineIds.Contains(a.PaymentClaimServiceLineId) && a.DateDeleted == null)
                .AsNoTracking()
                .Select(a => new AdjustmentDto
                {
                    PaymentClaimServiceLineId = a.PaymentClaimServiceLineId,
                    AdjustmentAmount = a.AdjustmentAmount,
                    IsAdjustmentPositive = a.IsAdjustmentPositive,
                    AdjustmentGroupCode = a.AdjustmentGroupCode,
                    DateDeleted = a.DateDeleted
                })
                .ToListAsync();

            // 4) load errors for these lines (optional)
            var errors = await _paymentClaimServiceLineErrorEntity.Query()
                .Where(e => serviceLineIds.Contains(e.PaymentClaimServiceLineId))
                .AsNoTracking()
                .Select(e => new ErrorDto
                {
                    PaymentClaimServiceLineId = e.PaymentClaimServiceLineId,
                    ErrorType = e.ErrorType
                })
                .ToListAsync();

            var adjustmentsByLine = adjustments.GroupBy(a => a.PaymentClaimServiceLineId)
                                               .ToDictionary(g => g.Key, g => g.ToList());

            var errorsByLine = errors.GroupBy(e => e.PaymentClaimServiceLineId)
                                     .ToDictionary(g => g.Key, g => g.ToList());

            var dtoList = serviceLines.Select(x => new PaymentGroupedModel
            {
                PaymentId = x.PaymentId,
                PaymentTypeId = x.PaymentTypeId,
                PaymentClaimId = x.PaymentClaimId ?? 0,
                ClaimId = x.ClaimId ?? 0,
                ChargeId = x.ClaimChargeEntryId ?? 0,
                PatientId = x.ChildProfileId,
                PatientName = x.PatientName?.Trim() ?? string.Empty,
                ServiceLineId = x.ServiceLineId,
                DateOfService = x.DateOfService,
                ServiceCode = x.ServiceCode,
                ChargeAmount = x.ChargeAmount,
                AllowedAmount = x.AllowedAmount,
                PaidAmount = x.PaidAmount,
                DateLastModified = x.DateLastModified,
                DateDeleted = x.DateDeleted,
                Adjustment = adjustmentsByLine.TryGetValue(x.ServiceLineId, out var adjList) ? adjList : new List<AdjustmentDto>(),
                HasErrors = errorsByLine.TryGetValue(x.ServiceLineId, out var errList) ? errList.Any() : false
            }).ToList();

            _allChargeDataCacheDictionary[cacheKey] = dtoList;

            _allChargeDataCached = dtoList;
        }

        private async Task<Dictionary<int, decimal>> GetChargeEntryWriteOffsBulkAsync(IEnumerable<int> chargeEntryIds)
        {
            var ids = (chargeEntryIds ?? Enumerable.Empty<int>()).Where(i => i > 0).Distinct().ToList();
            if (!ids.Any()) return new Dictionary<int, decimal>();

            // Determine which ids we still need to query
            var missing = ids.Where(id => !_writeOffsCachedIds.Contains(id)).ToList();

            if (missing.Any())
            {
                var grouped = await _claimChargeEntryWriteOffRepository.Query()
                    .Where(x => missing.Contains(x.ClaimChargeEntryId) && x.DateDeleted == null)
                    .GroupBy(x => x.ClaimChargeEntryId)
                    .Select(g => new
                    {
                        ChargeEntryId = g.Key,
                        TotalWriteOff = g.Sum(x => x.WriteOffAmount) ?? 0m
                    })
                    .ToListAsync();

                // populate caches
                foreach (var w in grouped)
                {
                    _writeOffsCache[w.ChargeEntryId] = w.TotalWriteOff;
                    _writeOffsCachedIds.Add(w.ChargeEntryId);
                }

                // ids with no rows => zero write-off
                var returned = new HashSet<int>(grouped.Select(x => x.ChargeEntryId));
                foreach (var id in missing.Except(returned))
                {
                    _writeOffsCache[id] = 0m;
                    _writeOffsCachedIds.Add(id);
                }
            }

            // Build result only for requested ids
            return ids.ToDictionary(i => i, i => _writeOffsCache.TryGetValue(i, out var v) ? v : 0m);
        }

        public async Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentLinkedServiceLinesAsyncNew(GetPatientPaymentServiceLinesModel model, bool isPatientDetailsLoading = false)
        {
            if (model.SortingModels.Count == 0)
                model.SortingModels.Add(new SortingModel { Field = "patientPayment", Dir = "desc" });

            var payment = await _paymentRepository.GetByIdAsync(model.PaymentId);

            // Get grouped data as dictionary for quick lookup            
            var groupedDict = await GetGroupedDictCachedAsync(payment, model.PatientId, 0, true, GroupByParam.Charge);

            // Ensure _allChargeDataCached is populated
            var allCharges = await GetAllChargesCachedAsync(payment.Id, model.PatientId, 0);

            var filteredData = (allCharges ?? Enumerable.Empty<PaymentGroupedModel>())
                                                                                   .Where(x => x.PatientId == model.PatientId && x.PaymentId == model.PaymentId)
                                                                                   .ToList();

            // Project initial rows
            var rows = filteredData.Select(x => new PaymentClaimServiceLineModel
            {
                Id = x.ServiceLineId,
                PatientId = x.PatientId,
                PatientName = x.PatientName,
                ChargeEntryId = x.ChargeId,
                DateOfService = x.DateOfService,
                Procedure = x.ServiceCode,
                AllowedAmount = x.AllowedAmount ?? 0,
                BilledAmount = x.ChargeAmount ?? 0,
                PaidAmount = x.PaidAmount ?? 0,
                Adjustment = 0,
                PatientResponsibility = 0,
                Balance = 0,
                Mods = string.Join(",", x.ProcedureModifier1, x.ProcedureModifier2, x.ProcedureModifier3, x.ProcedureModifier4),
                DateLastModified = x.DateLastModified,
                ClaimId = x.PaymentClaimId,
                HasErrors = x.HasErrors,
                IsLinked = (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) && x.PaidAmount > 0
            }).ToList();

            // Prepare placeholder for no-result safely
            PaymentClaimServiceLineModel dataForNoResult = null;
            if (rows.Any())
                dataForNoResult = rows.First();
            else if (isPatientDetailsLoading && filteredData.Any())
            {
                var f = filteredData.First();
                dataForNoResult = new PaymentClaimServiceLineModel
                {
                    PatientId = f.PatientId,
                    PatientName = f.PatientName,
                    ChargeEntryId = f.ChargeId,
                    DateOfService = f.DateOfService,
                    Procedure = f.ServiceCode,
                    BilledAmount = f.ChargeAmount ?? 0,
                    DateLastModified = f.DateLastModified
                };
            }

            // Only linked rows
            var result = rows.Where(x => x.IsLinked).ToList();

            // If no linked rows and we need placeholder
            if (result.Count == 0 && isPatientDetailsLoading && dataForNoResult != null)
            {
                dataForNoResult.PatientResponsibility = 0;
                dataForNoResult.Adjustment = 0;
                dataForNoResult.InsurancePayment = 0;
                dataForNoResult.PatientPayment = 0;
                dataForNoResult.Balance = 0;
                dataForNoResult.ExpectedAmount = 0;
                dataForNoResult.PatientResponsibilityBalance = 0;
                result.Add(dataForNoResult);
            }
            else if (result.Count > 0)
            {
                // Bulk operations: collect ids and fetch modifiers + write-offs in single queries
                var chargeIds = result.Select(r => r.ChargeEntryId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();

                // Bulk modifiers
                var modifiers = await _claimChargeEntryRepository.Query()
                    .Where(c => chargeIds.Contains(c.Id))
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.Modifier1, c.Modifier2, c.Modifier3, c.Modifier4 })
                    .ToListAsync();
                var modsById = modifiers.ToDictionary(m => m.Id, m => string.Join(",", m.Modifier1, m.Modifier2, m.Modifier3, m.Modifier4));

                // Bulk write-offs (populates internal cache)
                var writeOffsById = await GetChargeEntryWriteOffsBulkAsync(chargeIds);

                // Now fill each row in-memory (no awaits in loop)
                foreach (var data in result)
                {
                    if (data.ChargeEntryId.HasValue && groupedDict.TryGetValue(data.ChargeEntryId.Value, out var paymentInfo))
                    {
                        data.PatientResponsibility = paymentInfo.PatientResponsibility;
                        data.Adjustment = paymentInfo.Adjustment;
                        data.Adjustment += writeOffsById.TryGetValue(data.ChargeEntryId.Value, out var w) ? w : 0m;
                        data.InsurancePayment = paymentInfo.InsurancePayment;
                        data.PatientPayment = paymentInfo.PatientPayment;

                        if (data.ChargeEntryId.HasValue && modsById.TryGetValue(data.ChargeEntryId.Value, out var mm))
                            data.Mods = string.Join(", ", mm.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)));

                        data.Balance = data.BilledAmount - data.InsurancePayment - data.Adjustment - data.PatientResponsibility;
                        data.ExpectedAmount = data.BilledAmount;
                        data.PatientResponsibilityBalance = paymentInfo.PatientResponsibilityBalance;
                    }
                    else
                    {
                        // fallback values if grouped info missing
                        data.PatientResponsibility = 0;
                        data.Adjustment = writeOffsById.TryGetValue(data.ChargeEntryId ?? 0, out var w) ? w : getChargeEntryWriteOff(data.ChargeEntryId ?? 0);
                        data.InsurancePayment = 0;
                        data.PatientPayment = 0;
                        data.Balance = data.BilledAmount;
                        data.ExpectedAmount = data.BilledAmount;
                        data.PatientResponsibilityBalance = 0;
                    }
                }
            }

            // Apply sorting/filtering/paging as before
            result = result.AsQueryable()
                           .OrderBy(model.SortingModels)
                           .Filter(model.FilterModels)
                           .ToList();

            return result;
        }

        public async Task<List<PaymentClaimServiceLineModel>> GetPatientPaymentUnlinkedServiceLinesAsyncNew(GetPatientPaymentServiceLinesModel model)
        {
            if (model.SortingModels.Count == 0)
                model.SortingModels.Add(new SortingModel { Field = "patientPayment", Dir = "desc" });

            var payment = await _paymentRepository.GetByIdAsync(model.PaymentId);

            // Build grouped dictionary keyed by ChargeId (fast lookups)         
            var groupedDict = await GetGroupedDictCachedAsync(payment, model.PatientId, 0, false, GroupByParam.Charge);

            var allCharges = await GetAllChargesCachedAsync(payment.Id, model.PatientId, 0);

            var filteredData = (allCharges ?? Enumerable.Empty<PaymentGroupedModel>())
                                                                                    .Where(x => x.PatientId == model.PatientId &&
                                                                                                x.PaymentId == model.PaymentId &&
                                                                                                x.DateOfService <= payment.DepositDate)
                                                                                    .ToList();

            // Project initial DTOs
            var initial = filteredData.Select(x => new PaymentClaimServiceLineModel
            {
                Id = x.ServiceLineId,
                PatientId = x.PatientId,
                PatientName = x.PatientName,
                ChargeEntryId = x.ChargeId,
                DateOfService = x.DateOfService,
                Procedure = x.ServiceCode,
                AllowedAmount = x.AllowedAmount ?? 0,
                BilledAmount = x.ChargeAmount ?? 0,
                PaidAmount = x.PaidAmount ?? 0,
                Adjustment = 0,
                PatientResponsibility = 0,
                Balance = 0,
                Mods = string.Join(",", x.ProcedureModifier1, x.ProcedureModifier2, x.ProcedureModifier3, x.ProcedureModifier4),
                DateLastModified = x.DateLastModified,
                ClaimId = x.PaymentClaimId,
                HasErrors = x.HasErrors,
                IsLinked = (x.PaymentTypeId == (int)PaymentTypes.ClientPayment || x.PaymentTypeId == (int)PaymentTypes.OtherPayment) && x.PaidAmount > 0
            }).ToList();

            // Keep only unlinked ones
            var result = initial.Where(x => !x.IsLinked).ToList();
            if (!result.Any())
            {
                // nothing to do — return empty list (previous behavior returned empty list)
                return new List<PaymentClaimServiceLineModel>();
            }

            // Bulk: collect charge ids used
            var chargeIds = result.Select(r => r.ChargeEntryId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();

            // Bulk fetch modifiers (one DB call)
            var modifiersLookup = await _claimChargeEntryRepository.Query()
                .Where(c => chargeIds.Contains(c.Id))
                .AsNoTracking()
                .Select(c => new { c.Id, c.Modifier1, c.Modifier2, c.Modifier3, c.Modifier4 })
                .ToListAsync();
            var modifiersById = modifiersLookup.ToDictionary(x => x.Id, x => string.Join(",", x.Modifier1, x.Modifier2, x.Modifier3, x.Modifier4));

            // Bulk fetch write-offs (single grouped DB call; populates internal cache too)
            var writeOffsById = await GetChargeEntryWriteOffsBulkAsync(chargeIds);

            // Populate each DTO in-memory (no awaits inside loop)
            foreach (var data in result)
            {
                if (data.ChargeEntryId.HasValue && groupedDict.TryGetValue(data.ChargeEntryId.Value, out var paymentInfo))
                {
                    data.PatientResponsibility = paymentInfo.PatientResponsibility;
                    data.Adjustment = paymentInfo.Adjustment;
                    // add write-off either from bulk map or fallback cache-call (should rarely fallback)
                    data.Adjustment += writeOffsById.TryGetValue(data.ChargeEntryId.Value, out var w) ? w : getChargeEntryWriteOff(data.ChargeEntryId ?? 0);
                    data.InsurancePayment = paymentInfo.InsurancePayment;
                    data.PatientPayment = paymentInfo.PatientPayment;

                    if (modifiersById.TryGetValue(data.ChargeEntryId.Value, out var mm))
                        data.Mods = string.Join(", ", mm.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)));

                    data.Balance = data.BilledAmount - data.InsurancePayment - data.Adjustment - data.PatientResponsibility;
                    data.ExpectedAmount = data.BilledAmount;
                    data.PatientResponsibilityBalance = paymentInfo.PatientResponsibilityBalance;
                }
                else
                {
                    // fallback when grouped info not found (defensive)
                    data.PatientResponsibility = 0;
                    data.Adjustment = writeOffsById.TryGetValue(data.ChargeEntryId ?? 0, out var w) ? w : getChargeEntryWriteOff(data.ChargeEntryId ?? 0);
                    data.InsurancePayment = 0;
                    data.PatientPayment = 0;
                    data.Balance = data.BilledAmount;
                    data.ExpectedAmount = data.BilledAmount;
                    data.PatientResponsibilityBalance = 0;
                }
            }

            // Apply ShowPaid filter (preserve previous semantics)
            result = model.ShowPaid
                ? result.Where(x => x.PatientResponsibility != 0 && x.PatientPayment > 0).ToList()
                : result.Where(x => x.PatientResponsibility != 0).ToList();

            // Final sorting/filtering/paging
            result = result.AsQueryable()
                .OrderBy(model.SortingModels)
                .Filter(model.FilterModels)
                .ToList();

            return result;
        }

        private void RemoveAllChargeMemoryCache(int paymentId, IEnumerable<int> patientIds)
        {
            foreach (var patientId in patientIds.Distinct())
            {
                var cacheKey = GetCacheKey(paymentId, patientId);
                _allChargeDataCacheDictionary.TryRemove(cacheKey, out _);
            }
        }

        private async Task InvalidatePaymentCacheAsync(int paymentId, IEnumerable<int> patientIds = null)
        {
            patientIds ??= Enumerable.Empty<int>();

            var keys = new HashSet<string>();

            // per-child keys
            foreach (var patientId in patientIds.Distinct())
            {
                keys.Add(AllChargesKey(paymentId, patientId));
                keys.Add(GroupedKey(paymentId, patientId, true));
                keys.Add(GroupedKey(paymentId, patientId, false));
            }

            // Execute removals in parallel
            var removeTasks = keys.Select(async key =>
            {
                await _cacheService.RemoveAsync(key).ConfigureAwait(false);

            });

            await Task.WhenAll(removeTasks).ConfigureAwait(false);

            // 🔥 IMPORTANT: clear in-memory cache also
            RemoveAllChargeMemoryCache(paymentId, patientIds);
        }
    }
}