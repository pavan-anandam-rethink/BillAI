using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Models.Clients.History;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Client
{
    public class ClientChargeHistoryService : BaseService, IClientChargeHistoryService
    {       
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionsRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity> _claimSearchRenderingProviderRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchLocationEntity> _claimSearchLocationRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _claimChargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimVersionEntity> _claimVersionRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, PatientInvoiceDetailsEntity> _patientInvoiceDetailsRepository;
        private readonly IRepository<BillingDbContext, PatientInvoiceEntity> _patientInvoiceRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> _paymentClaimServiceLineAdjustmentRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> _claimChargeEntryWriteOffRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IPatientInvoiceService _invoiceService;

        public ClientChargeHistoryService(
            IRethinkMasterDataMicroServices rethinkServices,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionsRepository,
            IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity> claimSearchRenderingProvidersRepository,
            IRepository<BillingDbContext, ClaimSearchLocationEntity> claimSearchLocationRepository,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
            IRepository<BillingDbContext, ClaimVersionEntity> claimVersionRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IRepository<BillingDbContext, PatientInvoiceDetailsEntity> patientInvoiceDetailsRepository,
            IRepository<BillingDbContext, PatientInvoiceEntity> patientInvoiceRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustmentRepository,
            IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository,
            IPaymentClaimService paymentClaimService,
            IPatientInvoiceService invoiceService)

        {
            _rethinkServices = rethinkServices;
            _claimRepository = claimRepository;
            _claimSubmissionsRepository = claimSubmissionsRepository;
            _claimSearchRenderingProviderRepository = claimSearchRenderingProvidersRepository;
            _claimSearchLocationRepository = claimSearchLocationRepository;
            _claimChargeEntryRepository = claimChargeEntryRepository;
            _claimVersionRepository = claimVersionRepository;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _patientInvoiceDetailsRepository = patientInvoiceDetailsRepository;
            _paymentClaimServiceLineAdjustmentRepository = paymentClaimServiceLineAdjustmentRepository;
            _patientInvoiceRepository = patientInvoiceRepository;
            _claimChargeEntryWriteOffRepository = claimChargeEntryWriteOffRepository;
            _chargeEntryRepository = chargeEntryRepository;
            _paymentClaimService = paymentClaimService;
            _invoiceService = invoiceService;
        }

        public async Task<List<int>> GetClientHistoryClaimAsync(int accountInfoId)
        {
            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(accountInfoId);
            var clientIds = clientUsersList.Select(x => x.Id).ToList();
            return clientIds;
        }

        public async Task<ClientHistoryResponseModel> GetClientRecordAsync(ClientHistoryRequest requestModel, ClientRecordFilterModel filterModel)
        {
            if (requestModel.SortingModels == null || requestModel.SortingModels.Count == 0)
            {
                requestModel.SortingModels = new List<SortingModel>
                {
                    new SortingModel
                    {
                        Dir = "desc",
                        Field = "ClientId"
                    }
                };
            }

            var clientUsersTask = _rethinkServices.GetChildProfilesForAccount(requestModel.AccountInfoId);
            var fundersTask = _rethinkServices.GetAllFundersForAccount(requestModel.AccountInfoId);
            var locationTask = _rethinkServices.GetProviderLocationList(requestModel.AccountInfoId);

            await Task.WhenAll(clientUsersTask, fundersTask, locationTask);

            var clientUserList = clientUsersTask.Result.ToList();
            var funderList = fundersTask.Result.ToList();
            var locationList = locationTask.Result.data;

            //var locationIds = locationList.Select(l => l.id).ToList();
            //if (filterModel?.LocationIds?.Count > 0)
            //{
            //    locationIds = locationIds.Intersect(filterModel.LocationIds).ToList();
            //}


            var joinedQuery = (
                from c in _claimRepository.Query()
                where (filterModel.ClientId == null || filterModel.ClientId.Count == 0 || filterModel.ClientId.Contains(c.ChildProfileId))
                        && (c.AccountInfoId == requestModel.AccountInfoId)
                        && (filterModel.LocationId == null || filterModel.LocationId.Count == 0 || filterModel.LocationId.Contains(c.ProviderLocationId.Value))
                        && (filterModel.FunderId == null || filterModel.FunderId.Count == 0 || filterModel.FunderId.Contains(c.PrimaryFunderId))

                join cce in _claimChargeEntryRepository.Query()
                    on c.Id equals cce.ClaimId into cceGroup
                from cce in cceGroup.DefaultIfEmpty()

                join cv in _claimVersionRepository.Query()
                    on c.Id equals cv.ClaimId into cvGroup
                from cv in cvGroup.DefaultIfEmpty()

                join pcsl in _paymentClaimServiceLineRepository.Query().Where(pcsl => pcsl.DateDeleted == null)
                    on (cce != null ? cce.Id : 0) equals pcsl.ClaimChargeEntryId into pcslGroup
                from pcsl in pcslGroup.DefaultIfEmpty()

                join pind in _patientInvoiceDetailsRepository.Query().AsNoTracking()
                    on (cce != null ? cce.Id : 0) equals pind.ChargeId into pindGroup
                from pind in pindGroup.DefaultIfEmpty()

                join cs in _claimSubmissionsRepository.Query().AsNoTracking()
                    on c.Id equals cs.ClaimId into csGroup
                from cs in csGroup.DefaultIfEmpty()

                select new
                {
                    c,
                    cce,
                    cv,
                    pcsl,
                    pind,
                    cs,
                    //chargeAmount = (pcsl != null && (pcsl.ChargeAmount ?? 0) != 0)
                    //    ? pcsl.ChargeAmount
                    //    : (cce != null ? cce.Charges : 0)
                })
                .GroupBy(x => x.c.ChildProfileId)
                .Select(g => new
                {
                    ClientId = g.Key,
                    SecondaryFunder = g.Select(x => x.c.SecondaryFunderId).Where(id => id != null).Distinct().ToList(),
                    PrimaryFunder = g.Select(x => x.c.PrimaryFunderId).FirstOrDefault(),
                    //TotalChargeAmount = g.Sum(x => x.chargeAmount ?? 0),
                    //TotalInsurancePaid = g.Sum(x => x.pcsl != null ? x.pcsl.PaymentAmount ?? 0 : 0),
                    //TotalPatientPaid = g.Sum(x => x.pind != null ? x.pind.PatientPayments : 0),
                    Location = g.Select(x => x.c.ProviderLocationId).Distinct(),
                    DateOfBirth = g.Select(x => x.cs.ChildProfileDOB).FirstOrDefault(),
                });



            if (filterModel?.DateOfBirth.HasValue ?? false)
            {
                clientUserList = clientUserList.Where(x => x.DateOfBirth.Date == filterModel.DateOfBirth.Value.Date).ToList();
            }

            var clientIds = clientUserList.Select(c => c.Id).ToList();
            joinedQuery = joinedQuery.Where(x => clientIds.Contains(x.ClientId));

            var total = joinedQuery.Count();

            var result = joinedQuery.ToList();

            var resultQuery = result.Select(x =>
            {
                var client = clientUserList.FirstOrDefault(y => y.Id == x.ClientId);

                return new ClientHistoryResponse
                {
                    ClientName = $"{client?.FirstName} {client?.MiddleName} {client?.LastName}".Trim(),
                    ClientId = x.ClientId.ToString(),
                    DateOfBirth = client.DateOfBirth,
                    Location = x.Location.Count() > 0 ? locationList.FirstOrDefault(l => l.id == x.Location.FirstOrDefault())?.name : "",
                    Gender = client?.GenderId == 1 ? "Male" : "Female" ?? "Others",
                    Address = $"{client?.Address}, {client?.Address2}",
                    Age = DateTime.Today.Year - client.DateOfBirth.Year,
                    PrimaryFunder = x.PrimaryFunder > 0 ? funderList.FirstOrDefault(f => f.id == x.PrimaryFunder)?.funderName : "",
                    SecondaryFunder = (x.SecondaryFunder != null && x.SecondaryFunder.Count > 0)
                         ? funderList.FirstOrDefault(f => x.SecondaryFunder.Contains(f.id))?.funderName ?? ""
                         : "",
                    //Billed = x.TotalChargeAmount,
                    //InsurancePaid = x.TotalInsurancePaid,
                    //PatientPaid = x.TotalPatientPaid,
                    //RemainingClaimBalance = x.TotalChargeAmount - x.TotalInsurancePaid - x.TotalPatientPaid
                };
            }).AsQueryable().OrderBy(requestModel.SortingModels);

            var data = requestModel?.Take == 0 ? resultQuery.Skip(requestModel.Skip).ToList().Distinct()
                 : resultQuery.Skip(requestModel.Skip).Take(requestModel.Take).ToList().Distinct();

            var response = new ClientHistoryResponseModel
            {
                clientHistoryResponse = data.ToList(),
                Total = total
            };

            return response;
        }

        public async Task<ClientHistoryChargeDetailsResponse> GetClientChargeHistoryDetailsAsync(ClientHistoryChargeDetailsRequest ClientHistoryChargeDetailsRequest, ClientHistoryChargeFilterModel ClientHistoryChargeFilterModel)
        {
            if (ClientHistoryChargeDetailsRequest.SortingModels == null || ClientHistoryChargeDetailsRequest.SortingModels.Count == 0)
            {
                ClientHistoryChargeDetailsRequest.SortingModels =
                [
                    new SortingModel
                    {
                        Dir = "desc",
                        Field = "dateOfService"
                    }
                ];
            }
            var model = (ClientHistoryChargeFilterModel == null || ClientHistoryChargeFilterModel.FromDate == null)
                        ? new ClientHistoryChargeFilterModel
                        {
                            ThroughDate = DateTime.Today,
                            FromDate = DateTime.Today.AddDays(-90)
                        }
                        : ClientHistoryChargeFilterModel;

            var unitTypes = _rethinkServices.GetUnitTypesAsync().GetAwaiter().GetResult();

            var chargeLevelQuery =
            from arcce in _claimChargeEntryRepository.Query().AsNoTracking()
            where arcce.DateDeleted == null && arcce.DateOfService.Date >= model.FromDate.Date && arcce.DateOfService.Date <= model.ThroughDate.Date
            join claim in _claimRepository.Query().AsNoTracking()
                on arcce.ClaimId equals claim.Id
            where claim.ChildProfileId == ClientHistoryChargeDetailsRequest.ClientId

            join loc in _claimSearchLocationRepository.Query().AsNoTracking()
                on claim.LocationCodeId equals loc.Id into locs
            from loc in locs.DefaultIfEmpty()

            join cv in _claimVersionRepository.Query().AsNoTracking()
                on arcce.ClaimId equals cv.ClaimId into cvs
            from cv in cvs.DefaultIfEmpty()
            join pcsl in _paymentClaimServiceLineRepository.Query().AsNoTracking().Where(x => x.DateDeleted == null)
                on arcce.Id equals pcsl.ClaimChargeEntryId into pcsls
            from pcsl in pcsls.DefaultIfEmpty()
            join pind in _patientInvoiceDetailsRepository.Query().AsNoTracking()
                on arcce.Id equals pind.ChargeId into pinds
            from pind in pinds.DefaultIfEmpty()
            join pi in _patientInvoiceRepository.Query().AsNoTracking()
                on pind.InvoiceId equals pi.Id into pis
            from pi in pis.DefaultIfEmpty()
            join renderingProviderTemp in _claimSearchRenderingProviderRepository.Query().AsNoTracking()
                on (claim.RenderingStaffMemberId == -2 || claim.RenderingStaffMemberId == null ? claim.MemberId : claim.RenderingStaffMemberId)
                equals renderingProviderTemp.Id into renderingProviders
            from renderingProvider in renderingProviders
                .Where(r => r.DateDeleted == null)
                .DefaultIfEmpty()
            join writeOff in _claimChargeEntryWriteOffRepository.Query().AsNoTracking()
                .Where(w => w.DateDeleted == null)
                on arcce.Id equals writeOff.ClaimChargeEntryId into writeOffs
            from writeOff in writeOffs.DefaultIfEmpty()

                //join paymentClaim in _paymentClaimRepository.Query().AsNoTracking()
                //    on pcsl.PaymentClaimId equals paymentClaim.Id into paymentClaims
                //from paymentClaim in paymentClaims.DefaultIfEmpty()
                //join payment in _paymentRepository.Query().AsNoTracking()
                //    on paymentClaim.PaymentId equals payment.Id into payments
                //from payment in payments.DefaultIfEmpty()
            select new ClientHistoryChargeDetailsModel
            {
                DateOfService = arcce.DateOfService,
                BillingCode = arcce.BillingCode,
                PlaceOfService = loc.Name,
                PlaceOfServiceId = claim.LocationCodeId,
                RenderingProvider = renderingProvider != null ? renderingProvider.Name : null,
                RenderingProviderId = renderingProvider != null ? renderingProvider.Id : null,
                AuthorizationNumber = claim.AuthorizationNumber,
                Modifiers = string.Join(",", new[] { arcce.Modifier1, arcce.Modifier2, arcce.Modifier3, arcce.Modifier4 }),
                Diagnosis = arcce.DiagnosisCode,
                PrimaryFunderId = claim.PrimaryFunderId,
                AuthorizationNumberId = claim.AuthorizationId,
                PrimaryFunder = "",
                PrimaryClaimID = claim.ClaimIdentifier.ToString(),
                ClaimStatus = claim.ClaimStatus.ToString(),
                UnitTypeId = arcce.UnitTypeId,
                Units = arcce.Units,
                PerUnitCharge = arcce.UnitRate,
                BilledAmount = (arcce.Units * arcce.UnitRate) ?? pcsl.ChargeAmount ?? 0,
                InsurancePayment = 0,//payment.PaymentTypeId == (int)PaymentTypes.InsurancePayment ? paymentClaim.TotalPayment : 0,
                PatientResponsibilityAdjustments = cv != null ? cv.PatientResponsibilityAmount : 0,
                ClaimBalance = 0,
                InvoiceNumber = pind != null ? pind.PatientInvoiceEntity.InvoiceNumber : null,
                InvoiceStatus = pi != null ? ((PatientInvoiceStatus)pi.Status).ToString() : string.Empty,
                PatientResponsibility = 0,
                PatientPayments = 0,
                PatientBalance = 0,
                PaymentClaimServiceLineId = pcsl.Id,
                WriteOffs = writeOff.WriteOffAmount,
                ChargeId = pind.ChargeId,
                ClaimChargeId = arcce.Id,
            };

            if (ClientHistoryChargeFilterModel?.PlaceOfService != null && ClientHistoryChargeFilterModel.PlaceOfService.Any())
            {
                chargeLevelQuery = chargeLevelQuery.Where(x => ClientHistoryChargeFilterModel.PlaceOfService.Contains(x.PlaceOfServiceId ?? 0));
            }

            if (ClientHistoryChargeFilterModel?.RenderingProvider != null && ClientHistoryChargeFilterModel.RenderingProvider.Any())
            {
                chargeLevelQuery = chargeLevelQuery.Where(x => ClientHistoryChargeFilterModel.RenderingProvider.Contains(x.RenderingProviderId ?? 0));

            }

            //if (!string.IsNullOrEmpty(ClientHistoryChargeFilterModel?.AuthorizationNumber))
            //{
            //    chargeLevelQuery = chargeLevelQuery.Where(x => x.AuthorizationNumber == ClientHistoryChargeFilterModel.AuthorizationNumber);
            //}

            if (ClientHistoryChargeFilterModel?.AuthorizationNumber != null && ClientHistoryChargeFilterModel.AuthorizationNumber.Any())
            {
                chargeLevelQuery = chargeLevelQuery.Where(x => ClientHistoryChargeFilterModel.AuthorizationNumber.Contains(x.AuthorizationNumberId ?? 0));
            }

            if (ClientHistoryChargeFilterModel?.PrimaryFunder != null && ClientHistoryChargeFilterModel.PrimaryFunder.Any())
            {
                chargeLevelQuery = chargeLevelQuery.Where(x => ClientHistoryChargeFilterModel.PrimaryFunder.Contains(x.PrimaryFunderId));
            }

            // getting the insurance payment
            var paymentClaimServiceLineIds = chargeLevelQuery.Select(x => x.PaymentClaimServiceLineId).Distinct().ToList();

            var paymentData = await _paymentClaimServiceLineRepository.Query()
                            .Include(x => x.PaymentClaim).ThenInclude(x => x.Payment)
                            .Include(x => x.PaymentClaimServiceLineAdjustments)
                            .Include(x => x.PaymentClaimServiceLineErrors)
                            .Where(x => paymentClaimServiceLineIds.Contains(x.Id) &&
                                        x.DateDeleted == null &&
                                        x.PaymentClaim.DateDeleted == null &&
                                        x.PaymentClaim.Claim.DateDeleted == null)
                            .ToListAsync();

            var data = chargeLevelQuery.ToList();

            // Get adjustments
            //var paymentClaimServiceLineIds = data.Select(x => x.PaymentClaimServiceLineId).ToList();

            // Get all funders
            var accountInfoId = await _claimRepository.Query()
                                    .Where(c => c.ChildProfileId == ClientHistoryChargeDetailsRequest.ClientId)
                                    .Select(c => c.AccountInfoId)
                                    .FirstOrDefaultAsync();
            var fundersList = await _rethinkServices.GetAllFundersForAccount(accountInfoId);

            // PaymentClaim Service lines
            var paymentClaimServiceLines = await _paymentClaimServiceLineRepository.Query()
                                                .Where(p => paymentClaimServiceLineIds.Contains(p.Id) && p.DateDeleted == null)
                                                .ToListAsync();

            // ServiceLine Adjustments
            var paymentClaimServiceLineAdjustments = await _paymentClaimServiceLineAdjustmentRepository.Query()
                                                .Where(p => paymentClaimServiceLineIds.Contains(p.PaymentClaimServiceLineId) && p.DateDeleted == null)
                                                .ToListAsync();

            //for (int i = 0; i < paymentClaimServiceLineIds.Count; i++)
            foreach (var item in data)
            {
                item.PrimaryFunder = fundersList.FirstOrDefault(f => f.id == item.PrimaryFunderId)?.funderName ?? "";

                if (item?.ClaimChargeId != null)
                {
                    item.PatientPayments = paymentClaimServiceLines.Where(x => x.ClaimChargeEntryId == item.ChargeId)
                                                .Sum(x => x.PaymentAmount);
                }
                item.UnitTypeValue = unitTypes.FirstOrDefault(x => x.id == item.UnitTypeId)?.unit ?? 0;
                var serviceLineId = item.PaymentClaimServiceLineId;

                var adjustments = paymentClaimServiceLineAdjustments.Where(p => p.PaymentClaimServiceLineId == serviceLineId).ToList();

                if (adjustments.Count > 0)
                {
                    var positiveAdj = adjustments.Where(a => a.IsAdjustmentPositive == true && a.AdjustmentGroupCode != "PR").Sum(a => a.AdjustmentAmount ?? 0);
                    var negativeAdj = adjustments.Where(a => (a.IsAdjustmentPositive == false || a.IsAdjustmentPositive == null) && a.AdjustmentGroupCode != "PR").Sum(a => a.AdjustmentAmount ?? 0);
                    var positivePR = adjustments.Where(a => a.IsAdjustmentPositive == true && a.AdjustmentGroupCode == "PR").Sum(a => a.AdjustmentAmount ?? 0);
                    var negativePR = adjustments.Where(a => (a.IsAdjustmentPositive == false || a.IsAdjustmentPositive == null) && a.AdjustmentGroupCode == "PR").Sum(a => a.AdjustmentAmount ?? 0);

                    item.Adjustments = positiveAdj - negativeAdj;
                    item.PatientResponsibility = positivePR - negativePR;
                }
            }

            var responseItems = data
                .GroupBy(item => item.ClaimChargeId)
                .Select(g =>
                {
                    var insurancePayment = paymentData.Where(x => (x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.InsurancePayment || x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.ERAReceived) &&
                                                            x.DateDeleted == null && x.ClaimChargeEntryId == g.Key).Sum(x => x.PaymentAmount);
                    var patientPayment = paymentData.Where(x => (x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.ClientPayment) &&
                                                            x.DateDeleted == null && x.ClaimChargeEntryId == g.Key).Sum(x => x.PaymentAmount);
                    return new ClientHistoryChargeDetails
                    {
                        ChargeId = g.Key,
                        DateOfService = g.First().DateOfService,
                        BillingCode = g.First().BillingCode,
                        PlaceOfService = g.First().PlaceOfService,
                        RenderingProvider = g.First().RenderingProvider,
                        AuthorizationNumber = g.First().AuthorizationNumber,
                        Modifiers = g.First().Modifiers.TrimEnd(','),
                        Diagnosis = g.First().Diagnosis,
                        PrimaryFunder = g.First().PrimaryFunder,
                        PrimaryClaimID = g.First().PrimaryClaimID,
                        ClaimStatus = g.First().ClaimStatus,
                        Hours = Convert.ToDouble((g.First().Units * g.First().UnitTypeValue) / 60),
                        Units = g.First().Units,
                        PerUnitCharge = g.First().PerUnitCharge,
                        BilledAmount = g.First().BilledAmount ?? 0,
                        InsurancePayment = insurancePayment,//g.Sum(x=> x.InsurancePayment ?? 0),
                        Adjustments = g.Sum(x => ((-x.WriteOffs ?? 0) + (x.Adjustments ?? 0))),
                        // Claim Balance = BilledAmount + InsurancePayment + Adjustments + PatientResponsibility (insurancePayment, Adjustment and PR are negative))
                        ClaimBalance = (g.First().BilledAmount ?? 0) + g.Sum(x => ((-x.WriteOffs ?? 0) + (x.Adjustments ?? 0))) - (insurancePayment ?? 0) + g.Sum(x => x.PatientResponsibility ?? 0),
                        InvoiceNumber = g.First().InvoiceNumber,
                        InvoiceStatus = g.First().InvoiceStatus,
                        PatientResponsibility = g.Sum(x => x.PatientResponsibility) ?? 0,
                        PatientPayments = patientPayment ?? 0,
                        PatientBalance = (patientPayment ?? 0) + g.Sum(x => (x.PatientResponsibility ?? 0))
                    };
                }).AsQueryable().OrderBy(ClientHistoryChargeDetailsRequest.SortingModels).ToList();

            var total = responseItems.Count();
            var response = ClientHistoryChargeDetailsRequest.Take == 0 ? responseItems.Skip(ClientHistoryChargeDetailsRequest.Skip).ToList()
                   : responseItems.Skip(ClientHistoryChargeDetailsRequest.Skip).Take(ClientHistoryChargeDetailsRequest.Take).ToList();

            return new ClientHistoryChargeDetailsResponse
            {
                ChargeDetails = response,
                Total = total
            };
        }

        public Task<List<AuthorizationNumberResponse>> GetAllAuthorizationNumbersAsync(UserInfo model)
        {
            var authorizationNumbers = _claimRepository.Query()
                                        .Where(c => c.AccountInfoId == model.AccountInfoId
                                            && c.DateDeleted == null && c.AuthorizationId != null
                                            && !string.IsNullOrEmpty(c.AuthorizationNumber))
                                        .Select(c => new AuthorizationNumberResponse
                                        {
                                            Id = c.AuthorizationId ?? 0,
                                            Name = c.AuthorizationNumber
                                        })
                                        .Distinct()
                                        .ToList();

            return Task.FromResult(authorizationNumbers);
        }

        public async Task<InvoiceHistoryResponseModel> InvoicesSearchAsync(InvoiceHistoryRequest requestModel, InvoiceHistoryRequestFilterModel filter)
        {
            // Early exit for null filter or missing client id
            if (filter == null || requestModel == null || requestModel.ClientId == 0)
                return new InvoiceHistoryResponseModel { Data = new List<InvoiceHistoryResponse>(), TotalCount = 0 };

            // Query invoices for the client and account
            var invoicesQuery = _patientInvoiceRepository.Query()
                .AsNoTracking()
                .Where(i => i.AccountId == filter.AccountInfoId && i.DateDeleted == null && i.ClientId == requestModel.ClientId);

            // Determine all defined status values to detect "Select All"
            var allDefinedStatuses = Enum.GetValues(typeof(PatientInvoiceStatus)).Cast<int>().ToHashSet();
            bool isSelectAll = filter.Status != null && allDefinedStatuses.SetEquals(filter.Status);

            // Apply status filter only when specific statuses are selected (not "Select All")
            if (filter.Status?.Count > 0 && !isSelectAll)
            {
                var statusIds = filter.Status.Select(s => (PatientInvoiceStatus)s).ToHashSet();
                invoicesQuery = invoicesQuery.Where(i => statusIds.Contains(i.Status));
            }

            // Apply invoice date filters
            if (filter.InvoiceDateFrom.HasValue)
            {
                var date = filter.InvoiceDateFrom.Value.Date;
                invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate.Date >= date);
            }
            if (filter.InvoiceDateTo.HasValue)
            {
                var date = filter.InvoiceDateTo.Value.Date;
                invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate.Date <= date);
            }
            if (filter.InvoiceDueDateFrom.HasValue)
            {
                var date = filter.InvoiceDueDateFrom.Value.Date;
                invoicesQuery = invoicesQuery.Where(i => i.PaymentDueDate.Date >= date);
            }
            if (filter.InvoiceDueDateTo.HasValue)
            {
                var date = filter.InvoiceDueDateTo.Value.Date;
                invoicesQuery = invoicesQuery.Where(i => i.PaymentDueDate.Date <= date);
            }

            // Get total count after filters
            int totalCount = await invoicesQuery.CountAsync();

            // Fetch invoices and details in parallel
            var invoicesTask = invoicesQuery.OrderBy(i => i.InvoiceDate).ToListAsync();
            var clientUsersTask = _rethinkServices.GetChildProfilesForAccount(filter.AccountInfoId);
            await Task.WhenAll(invoicesTask, clientUsersTask);
            var invoices = invoicesTask.Result;
            var invoiceIds = invoices.Select(i => i.Id).ToList();

            // Fetch invoice details and related data
            var invoiceDetails = await _patientInvoiceDetailsRepository
                .Query()
                .AsNoTracking()
                .Where(d => invoiceIds.Contains(d.InvoiceId) && d.DateDeleted == null)
                .Include(d => d.ChargeEntry)
                .ThenInclude(ce => ce.Claim)
                .ToListAsync();

            // Preload charge entry ids and details
            var chargeEntryIds = invoiceDetails.Select(d => d.ChargeId).Distinct().ToList();
            var chargeEntries = await getChargeDetails(chargeEntryIds);
            var chargeEntryLookup = chargeEntries.ToDictionary(c => c.Id);

            // Filter by date of service if needed
            if (filter.DateOfServiceFrom.HasValue || filter.DateOfServiceTo.HasValue)
            {
                invoiceDetails = invoiceDetails.Where(d =>
                {
                    if (!chargeEntryLookup.TryGetValue(d.ChargeId, out var ce)) return false;
                    return (!filter.DateOfServiceFrom.HasValue || ce.DateOfService >= filter.DateOfServiceFrom.Value) &&
                           (!filter.DateOfServiceTo.HasValue || ce.DateOfService <= filter.DateOfServiceTo.Value);
                }).ToList();
            }

            // Preload rendering providers and place of services
            var renderingProviders = _claimSearchRenderingProviderRepository.Query()
                .AsNoTracking()
                .Where(rp => rp.DateDeleted == null)
                .ToList();
            var placeOfServices = _claimSearchLocationRepository.Query()
                .AsNoTracking()
                .Where(pos => pos.DateDeleted == null)
                .ToList();

            // Preload payment/adjustment data
            var groupByChargeData = await _paymentClaimService.GetGroupedByPaymentsForPatientInvoice(chargeEntryIds);
            var chargeAmountsLookup = groupByChargeData.ToDictionary(x => x.ChargeId);

            // Build invoice response list
            var allBillingDetails = new List<InvoiceHistoryResponse>(invoiceDetails.Count);
            foreach (var invoice in invoices)
            {
                var details = invoiceDetails.Where(d => d.InvoiceId == invoice.Id).ToList();
                foreach (var d in details)
                {
                    var chargeEntry = chargeEntryLookup.GetValueOrDefault(d.ChargeId);
                    chargeAmountsLookup.TryGetValue(d.ChargeId, out var chargeAmounts);
                    var claim = d.ChargeEntry?.Claim;

                    var placeOfServiceName = claim != null
                        ? placeOfServices.FirstOrDefault(pos => pos.Id == claim.LocationCodeId)?.Name
                        : null;
                    var renderingProviderName = claim != null
                        ? renderingProviders.FirstOrDefault(rp =>
                            (claim.RenderingStaffMemberId == -2 || claim.RenderingStaffMemberId == null)
                                ? rp.Id == claim.MemberId
                                : rp.Id == claim.RenderingStaffMemberId)?.Name
                        : null;

                    // Resolve status display: handle undefined/unmapped enum values as "N/A"
                    string statusDisplay;
                    if (Enum.IsDefined(typeof(PatientInvoiceStatus), invoice.Status))
                    {
                        statusDisplay = GetEnumDescription(invoice.Status);
                    }
                    else
                    {
                        statusDisplay = "N/A";
                    }

                    var response = new InvoiceHistoryResponse
                    {
                        Id = chargeEntry?.Id ?? 0,
                        ClientId = invoice.ClientId,
                        BillingCode = chargeEntry?.BillingCode ?? string.Empty,
                        BilledAmount = Math.Round(chargeEntry?.BilledAmount ?? 0, 2),
                        Adjustments = Math.Round((chargeAmounts?.Adjustment ?? 0) + (chargeEntry?.WriteOffAmount ?? 0), 2),
                        AdjustmentsPR = Math.Round(chargeAmounts?.PatientResponsibility ?? 0, 2),
                        InsurancePayments = Math.Round(chargeAmounts?.InsurancePayment ?? 0, 2),
                        PatientPayments = Math.Round(chargeAmounts?.PatientPayment ?? 0, 2),
                        PatientBalance = Math.Round(chargeAmounts?.PatientResponsibilityBalance ?? 0, 2),
                        DateOfService = chargeEntry?.DateOfService.ToString("MM/dd/yyyy"),
                        InvoiceNumber = invoice.InvoiceNumber,
                        InvoiceDate = invoice.InvoiceDate.ToString("MM/dd/yyyy"),
                        PaymentDue = invoice.PaymentDueDate.ToString("MM/dd/yyyy"),
                        Status = statusDisplay,
                        PlaceOfService = placeOfServiceName,
                        RenderingProvider = renderingProviderName
                    };
                    allBillingDetails.Add(response);
                }
            }

            // Filter by patient responsibility (AdjustmentsPR) if needed
            if (filter.PatientResponsibilityFrom.HasValue || filter.PatientResponsibilityTo.HasValue)
            {
                allBillingDetails = allBillingDetails.Where(d =>
                    (!filter.PatientResponsibilityFrom.HasValue || d.AdjustmentsPR >= filter.PatientResponsibilityFrom.Value) &&
                    (!filter.PatientResponsibilityTo.HasValue || d.AdjustmentsPR <= filter.PatientResponsibilityTo.Value))
                    .ToList();
            }

            // Patient Balance filter
            if (filter.PatientBalanceFrom.HasValue || filter.PatientBalanceTo.HasValue)
            {
                allBillingDetails = allBillingDetails.Where(d =>
                    (!filter.PatientBalanceFrom.HasValue || d.PatientBalance >= filter.PatientBalanceFrom.Value) &&
                    (!filter.PatientBalanceTo.HasValue || d.PatientBalance <= filter.PatientBalanceTo.Value))
                    .ToList();
            }

            totalCount = allBillingDetails.Count;

            #region Ready to Invoice           
            // Handle Ready to Invoice status — include when Select All or when ReadytoInvoice is explicitly selected or no status filter
            if ((isSelectAll || filter.Status?.Contains((int)PatientInvoiceStatus.ReadytoInvoice) == true || filter.Status?.Count == 0) && !filter.InvoiceDateFrom.HasValue && !filter.InvoiceDateTo.HasValue && !filter.InvoiceDueDateFrom.HasValue && !filter.InvoiceDueDateTo.HasValue)
            {
                var createInvoiceFilters = new CreateInvoiceFilters
                {
                    AccountInfoId = filter.AccountInfoId,
                    Skip = 0,
                    Take = int.MaxValue,
                    Filters = new CreateInvoice
                    {
                        ClientIds = requestModel.ClientId.ToString(),
                        PatientResponsibilityFrom = filter.PatientResponsibilityFrom,
                        PatientResponsibilityTo = filter.PatientResponsibilityTo,
                        DateOfServiceFrom = filter.DateOfServiceFrom,
                        DateOfServiceTo = filter.DateOfServiceTo
                    },
                };

                var (readyToInvoicesResult, readyToInvoicesTotalcount) = await _invoiceService.GetPICreationDetails(createInvoiceFilters).ConfigureAwait(false);
                IEnumerable<PatientInvoiceCreationModel> filteredReadyToInvoices = readyToInvoicesResult;
                if (filter.PatientBalanceFrom.HasValue && filter.PatientBalanceTo.HasValue)
                {
                    filteredReadyToInvoices = filteredReadyToInvoices.Where(d =>
                        d.PatientBalance >= filter.PatientBalanceFrom.Value &&
                        d.PatientBalance <= filter.PatientBalanceTo.Value)
                        .ToList();
                    readyToInvoicesTotalcount = filteredReadyToInvoices.Count();
                }
                else if (filter.PatientBalanceFrom.HasValue)
                {
                    filteredReadyToInvoices = filteredReadyToInvoices.Where(d =>
                        d.PatientBalance >= filter.PatientBalanceFrom.Value)
                        .ToList();
                    readyToInvoicesTotalcount = filteredReadyToInvoices.Count();
                }
                else if (filter.PatientBalanceTo.HasValue)
                {
                    filteredReadyToInvoices = filteredReadyToInvoices.Where(d =>
                        d.PatientBalance <= filter.PatientBalanceTo.Value)
                        .ToList();
                    readyToInvoicesTotalcount = filteredReadyToInvoices.Count();
                }

                var relevantClaimIds = filteredReadyToInvoices.Select(x => x.ClaimId).Distinct().ToList();
                var claimChargeEntry = await _claimChargeEntryRepository
                    .Query()
                    .AsNoTracking()
                    .Where(d => d.DateDeleted == null && relevantClaimIds.Contains(d.ClaimId))
                    .Select(c => c.Claim)
                    .ToListAsync();

                var mappedInvoices = MapToInvoiceHistoryResponseList(
                    filteredReadyToInvoices.ToList(),
                    placeOfServices,
                    renderingProviders,
                    claimChargeEntry
                );
                allBillingDetails.AddRange(mappedInvoices);
                totalCount += readyToInvoicesTotalcount;
            }
            #endregion

            // Default sorting by dateOfService desc
            if (requestModel.SortingModels == null || requestModel.SortingModels.Count == 0)
            {
                requestModel.SortingModels =
                [
                    new SortingModel
                    {
                        Dir = "desc",
                        Field = "dateOfService"
                    }
                ];
            }

            if (requestModel.SortingModels?.Count > 0)
                allBillingDetails = allBillingDetails.AsQueryable().OrderBy(requestModel.SortingModels).ToList();

            // Paging
            if (requestModel.Take == 0)
                return new InvoiceHistoryResponseModel { Data = allBillingDetails, TotalCount = totalCount };
            if (totalCount >= requestModel.Take + requestModel.Skip)
                return new InvoiceHistoryResponseModel { Data = allBillingDetails.Skip(requestModel.Skip).Take(requestModel.Take).ToList(), TotalCount = totalCount };
            if (totalCount > requestModel.Take)
                return new InvoiceHistoryResponseModel { Data = allBillingDetails.Skip(requestModel.Skip).ToList(), TotalCount = totalCount };

            return new InvoiceHistoryResponseModel { Data = allBillingDetails, TotalCount = totalCount };
        }

        public static List<InvoiceHistoryResponse> MapToInvoiceHistoryResponseList(List<PatientInvoiceCreationModel> models, List<ClaimSearchLocationEntity> placeOfServices,List<ClaimSearchRenderingProviderEntity> renderingProviders,List<ClaimEntity> claimEntities)
        {
            var result = new List<InvoiceHistoryResponse>();
            foreach (var model in models)
            {
                var claim = claimEntities.FirstOrDefault(c => c.Id == model.ClaimId);
                var placeOfServiceName = claim != null
                    ? placeOfServices.FirstOrDefault(pos => pos.Id == claim.LocationCodeId)?.Name
                    : null;
                var renderingProviderName = claim != null
                    ? renderingProviders.FirstOrDefault(rp =>
                        (claim.RenderingStaffMemberId == -2 || claim.RenderingStaffMemberId == null)
                            ? rp.Id == claim.MemberId
                            : rp.Id == claim.RenderingStaffMemberId)?.Name
                    : null;

                result.Add(new InvoiceHistoryResponse
                {
                    Id = model.Id,
                    ClientId = model.ClientId,
                    BillingCode = model.BillingCode,
                    DateOfService = model.DateOfService,
                    BilledAmount = model.Charges,
                    Adjustments = model.Adjustment_Non_Patient_responsibility,
                    AdjustmentsPR = model.Adjustment_Patient_responsibility,
                    InsurancePayments = model.InsuranceAmount,
                    PatientPayments = model.PatientAmount,
                    PatientBalance = model.PatientBalance,
                    Status = model.Invoicestatus,
                    InvoiceNumber = null,
                    InvoiceDate = null,
                    PaymentDue = null,
                    PlaceOfService = placeOfServiceName,
                    RenderingProvider = renderingProviderName
                });
            }
            return result;
        }

        private async Task<List<ChargeDetails>> getChargeDetails(List<int> chargeEntryIds)
        {
            var chargeEntries = await _chargeEntryRepository
                            .Query()
                            .Where(c => chargeEntryIds.Contains(c.Id))
                            .Include(c => c.ClaimChargeEntryWriteOffs)
                            .Select(c => new ChargeDetails
                            {
                                Id = c.Id,
                                BillingCode = c.BillingCode,
                                Units = c.Units,
                                DateOfService = c.DateOfService,
                                BilledAmount = c.Charges,
                                WriteOffAmount = c.ClaimChargeEntryWriteOffs.Where(x => x.DateDeleted == null).Sum(x => x.WriteOffAmount).Value
                            })
                            .ToListAsync();
            return chargeEntries;
        }
    }
}
