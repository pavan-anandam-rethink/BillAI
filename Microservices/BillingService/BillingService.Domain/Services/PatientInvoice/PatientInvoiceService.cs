using BillingService.Domain.Interfaces;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Cache;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.PatientInvoice
{
    public sealed class PatientInvoiceService : BaseService, IPatientInvoiceService
    {
        private readonly IRazorViewService _razorViewService;
        private readonly IRepository<BillingDbContext, PatientInvoiceEntity> _patientInvoiceRepository;
        private readonly IRepository<BillingDbContext, PatientInvoiceDetailsEntity> _patientInvoiceDetailsRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchClientEntity> _claimsSearchClientsRepository;
        private readonly ILogger<PatientInvoiceService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPdfService _PdfService;
        private readonly ICacheService _CacheService;
        private readonly IClientInfoService _clientInfoService;
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly ICacheManager _cacheManager;
        private readonly string _invoiceMessage;
        private readonly string _invoiceRemark;
        private readonly int _cacheExpiration;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IRepository<BillingDbContext, PatientGuarantorEntity> _patientGuarantorRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> _claimChargeEntryWriteOffEntity;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;

        public PatientInvoiceService(
            IRazorViewService razorViewService,
            IRepository<BillingDbContext, PatientInvoiceEntity> patientInvoiceRepository,
            IRepository<BillingDbContext, PatientInvoiceDetailsEntity> patientInvoiceDetailsRepository,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository,
            IRepository<BillingDbContext, ClaimSearchClientEntity> claimsSearchClientsRepository,
            IConfiguration configuration,
            ILogger<PatientInvoiceService> logger,
            IPdfService PdfService,
            ICacheService CacheService,
            IClientInfoService clientInfoService,
            IPaymentClaimService paymentClaimService,
            ICacheManager cacheManager,
            IRethinkMasterDataMicroServices rethinkServices,
            IRepository<BillingDbContext, PatientGuarantorEntity> patientGuarantorRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffEntity,
            IRepository<BillingDbContext, ClaimEntity> claimRepository
            )
        {
            _razorViewService = razorViewService;
            _patientInvoiceRepository = patientInvoiceRepository;
            _patientInvoiceDetailsRepository = patientInvoiceDetailsRepository;
            _chargeEntryRepository = chargeEntryRepository;
            _configuration = configuration;
            _logger = logger;
            _PdfService = PdfService;
            _CacheService = CacheService;
            _clientInfoService = clientInfoService;
            _paymentClaimService = paymentClaimService;
            _invoiceMessage = Convert.ToString(_configuration.GetSection("invoice_message").Value);
            _invoiceRemark = Convert.ToString(_configuration.GetSection("invoice_remark").Value);
            _cacheExpiration = Convert.ToInt16(_configuration.GetSection("cacheExpiretime").Value ?? "8");
            _cacheManager = cacheManager;
            _rethinkServices = rethinkServices;
            _claimsSearchClientsRepository = claimsSearchClientsRepository;
            _patientGuarantorRepository = patientGuarantorRepository;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _claimChargeEntryWriteOffEntity = claimChargeEntryWriteOffEntity;
            _claimRepository = claimRepository;
        }

        public async Task<(IEnumerable<PatientInvoiceCreationModel> Data, int TotalCount)> GetPICreationDetails(CreateInvoiceFilters model)
        {
            _logger.LogInformation("GetPICreationDetails started. AccountInfoId={AccountInfoId}", model.AccountInfoId);

            var invoiceDetails = await _patientInvoiceDetailsRepository
                .Query()
                .AsNoTracking()
                .Where(d => d.DateDeleted == null && d.PatientInvoiceEntity.DateDeleted == null && d.PatientInvoiceEntity.AccountId == model.AccountInfoId)
                .Select(x => x.ChargeId)
                .ToListAsync();

            if (invoiceDetails.Count == 0)
            {
                _logger.LogInformation($"No invoice details found. AccountInfoId= {model.AccountInfoId}");
                invoiceDetails = await _chargeEntryRepository.Query().AsNoTracking()
                    .Where(c => c.Claim.AccountInfoId == model.AccountInfoId)
                    .Select(c => c.Id)
                    .Distinct()
                    .ToListAsync();
            }

            _logger.LogInformation("_patientInvoiceDetailsRepository completed. AccountInfoId={AccountInfoId}, InvoiceChargeCount={InvoiceChargeCount}", model.AccountInfoId, invoiceDetails.Count);

            var paymentChargeEntryIds = await _paymentClaimService.GetAllPaymentChargeIds(model);

            var filteredPayments = paymentChargeEntryIds.AsQueryable();

            paymentChargeEntryIds = filteredPayments.ToList();
            var invoicedChargeIds = invoiceDetails.ToHashSet();
            var chargeEntryIds = paymentChargeEntryIds
                .Where(c => c.ChargeId > 0 && !invoicedChargeIds.Contains(c.ChargeId.Value))
                .Select(c => c.ChargeId.Value)
                .Distinct()
                .ToList();

            // This is we need for Private Pay and if any account dont have any Invoice Created yet.
            var hasClaimData = _claimRepository.Query().Any(x => x.DateDeleted == null);

            if (chargeEntryIds.Count == 0 && 
                (!hasClaimData || (model.Filters?.DateOfServiceFrom == null && model.Filters?.DateOfServiceTo == null) 
                || paymentChargeEntryIds.Count > 0))
            {
                _logger.LogInformation("For new Account first Private Pay transection use the same invoiceDetails");
                chargeEntryIds = invoiceDetails;
            }

            _logger.LogInformation("Filtered charge entries identified. AccountInfoId={AccountInfoId}, ChargeEntryCount={ChargeEntryCount}",model.AccountInfoId,chargeEntryIds.Count);

            var chargeEntries = await getChargeDetails(chargeEntryIds);

            var chargeEntryLookup = chargeEntries.ToDictionary(c => c.Id);

            _logger.LogInformation("GetGroupedByPaymentsForPatientInvoice started. AccountInfoId={AccountInfoId}",model.AccountInfoId);

            var groupedChargeDetails = await _paymentClaimService.GetGroupedByPaymentsForPatientInvoice(chargeEntryIds);

            _logger.LogInformation("GetGroupedByPaymentsForPatientInvoice completed. AccountInfoId={AccountInfoId}, GroupedCount={GroupedCount}",model.AccountInfoId,groupedChargeDetails.Count);

            if (model.Filters.PatientResponsibilityFrom.HasValue || model.Filters.PatientResponsibilityTo.HasValue)
            {
                groupedChargeDetails = groupedChargeDetails.Where(d =>
                    (!model.Filters.PatientResponsibilityFrom.HasValue || d.PatientResponsibility >= model.Filters.PatientResponsibilityFrom.Value) &&
                    (!model.Filters.PatientResponsibilityTo.HasValue || d.PatientResponsibility <= model.Filters.PatientResponsibilityTo.Value))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(model.Filters.ClientIds)) // client id filter...
            {
                List<int> clientIds = model.Filters.ClientIds.Split(',').Select(i => Convert.ToInt32(i)).ToList();
                paymentChargeEntryIds = paymentChargeEntryIds.Where(c => clientIds.Contains(c.ClientId)).ToList();
            }

            if (model.Filters.DateOfServiceFrom.HasValue && model.Filters.DateOfServiceTo.HasValue)
            {
                var fromDate = ConvertToCompleteDate(model.Filters.DateOfServiceFrom.Value);
                var toDate = ConvertToCompleteDate(model.Filters.DateOfServiceTo.Value);
                paymentChargeEntryIds = paymentChargeEntryIds.Where(d =>
                    d.DateOfService >= fromDate &&
                    d.DateOfService <= toDate)
                    .ToList();
            }
            else if (model.Filters.DateOfServiceFrom.HasValue)
            {
                var fromDate = ConvertToCompleteDate(model.Filters.DateOfServiceFrom.Value);
                paymentChargeEntryIds = paymentChargeEntryIds.Where(d =>
                    d.DateOfService >= fromDate)
                    .ToList();
            }
            else if (model.Filters.DateOfServiceTo.HasValue)
            {
                var toDate = ConvertToCompleteDate(model.Filters.DateOfServiceTo.Value);
                paymentChargeEntryIds = paymentChargeEntryIds.Where(d =>
                    d.DateOfService <= toDate)
                    .ToList();
            }

            _logger.LogInformation("GetClientDetailsGuarantor started. AccountInfoId={AccountInfoId}",model.AccountInfoId);

            var clientNamesMap =
                await _rethinkServices.GetClientDetailsGuarantor(model.AccountInfoId);

            var clientNamesLookup = clientNamesMap?.ToDictionary(x => x.UserId);

            var filteredChargeIds = paymentChargeEntryIds.Select(c => c.ChargeId).Distinct().ToHashSet();

            var result = groupedChargeDetails
                .Where(g => g.PatientResponsibility != 0 && g.PatientResponsibilityBalance != 0 
                    && filteredChargeIds.Contains( g.ChargeId))
                .AsParallel()
                .Select(x =>
                {
                    var chargeDetails = chargeEntryLookup.GetValueOrDefault(x.ChargeId);
                    string guarantorName = "Missing Guarantor";
                    if (clientNamesLookup != null && clientNamesLookup.TryGetValue(x.PatientId, out var clientDetail) && clientDetail?.Address != null)
                    {
                        var name = clientDetail.Name;
                        guarantorName = $"{name.FirstName} {name.MiddleName} {name.LastName}";
                    }

                    return new PatientInvoiceCreationModel
                    {
                        Id = x.ChargeId,
                        ClaimId = x.ClaimId,
                        ClientId = x.PatientId,
                        ClientName = x.PatientName.Replace("  ", " "),
                        GuarantorName = guarantorName,
                        BillingCode = chargeDetails?.BillingCode,
                        DateOfService = x.DateOfService.ToString("MM/dd/yyyy"),
                        Units = Math.Round(chargeDetails?.Units ?? 0, 2),
                        Charges = Math.Round(chargeDetails?.BilledAmount ?? 0, 2),
                        InsuranceAmount = Math.Round(x.InsurancePayment ?? 0, 2),
                        Adjustment_Non_Patient_responsibility = Math.Round((x.Adjustment ?? 0) + (chargeDetails?.WriteOffAmount ?? 0), 2),
                        Adjustment_Patient_responsibility = Math.Round(x.PatientResponsibility ?? 0, 2),
                        PatientAmount = Math.Round(x.PatientPayment ?? 0, 2),
                        PatientBalance = Math.Round(x.PatientResponsibilityBalance ?? 0, 2),
                        Invoicestatus = GetEnumDescription(PatientInvoiceStatus.ReadytoInvoice)
                    };
                })
                .ToList();

            _logger.LogInformation( "GetPICreationDetails completed. AccountInfoId={AccountInfoId}, ResultCount={ResultCount}",model.AccountInfoId,result.Count);
            if (!result.Any())
            {
                return (result, 0);
            }

            return (result, result.Count);
        }

        public async Task<(byte[] PdfData, List<string> ErrorList)> GeneratePDF(List<InvoiceRequestModel> invoiceRequests, bool isSubmit, bool includePreviousInvoices, string invoiceNumber)
        {
            var combinedHtml = new StringBuilder();
            var invoiceNumbers = new List<string>();
            var errorList = new List<string>();
            try
            {
                var groupedCharges = invoiceRequests
                    .SelectMany(r => r.Charges, (r, c) => new { r.ClientId, r.AccountId, Charge = c })
                    .GroupBy(g => g.ClientId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Charge).ToList());

                var clientAccountMap = invoiceRequests
                    .GroupBy(r => r.ClientId)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.AccountId ?? 0);

                foreach (var group in groupedCharges)
                {
                    var invoices = new List<PatientInvoiceViewModel>();
                    var clientId = group.Key;
                    var charges = group.Value;
                    var accountId = clientAccountMap[clientId];

                    try
                    {
                        var chargeEntriesTask = await getChargeDetails(charges.Select(d => d.ChargeId).Distinct().ToList());
                        var chargeEntryLookup = chargeEntriesTask.ToDictionary(c => c.Id);
                        var (clientInfo, billingProviderInfo) = await GetClientBillingProviderInfoFromCache(accountId, clientId);

                        if (includePreviousInvoices)
                        {
                            var previousInvoices = await GetPreviousInvoices(accountId, clientId, null);
                            invoices.AddRange(previousInvoices);
                        }
                        if (isSubmit)
                        {
                            var currentInvoice = await GenerateInvoice(accountId, clientId, charges);
                            invoiceNumbers.Add(currentInvoice.InvoiceNumber);
                            invoices.Add(currentInvoice);
                        }
                        else
                        {
                            var groupByChargeData = await _paymentClaimService.GetGroupedByPaymentsForPatientInvoice(charges.Select(x => x.ChargeId).ToList());
                            var billingDetailsList = charges.Select(c =>
                            {
                                var chargeEntry = chargeEntryLookup.GetValueOrDefault(c.ChargeId);
                                var chargeAmounts = groupByChargeData.FirstOrDefault(x => x.ChargeId == c.ChargeId);
                                return new BillingDetailViewModel
                                {
                                    BillingCode = chargeEntry?.BillingCode,
                                    Units = Math.Round(chargeEntry?.Units ?? 0, 2),
                                    DateOfService = chargeEntry?.DateOfService.ToString("MM/dd/yyyy"),
                                    BilledAmount = Math.Round(chargeEntry?.BilledAmount ?? 0, 2),
                                    Adjustments = Math.Round((chargeAmounts?.Adjustment ?? 0) + (chargeEntry?.WriteOffAmount ?? 0), 2),
                                    AdjustmentsPR = Math.Round(chargeAmounts?.PatientResponsibility ?? 0, 2),
                                    InsurancePayments = Math.Round(chargeAmounts?.InsurancePayment ?? 0, 2),
                                    PatientPayments = Math.Round(chargeAmounts?.PatientPayment ?? 0, 2),
                                    PatientBalance = Math.Round(chargeAmounts?.PatientResponsibilityBalance ?? 0, 2)
                                };
                            }).ToList();

                            var defaultInvoice = new PatientInvoiceViewModel
                            {
                                InvoiceNumber = null,
                                InvoiceDate = null,
                                PaymentDue = null,
                                BillingDetails = billingDetailsList
                            };
                            invoices.Add(defaultInvoice);
                        }

                        var clientNamesMapForPdf = await _rethinkServices.GetClientDetailsGuarantor(accountId);
                        GuarantorInfo guarantor = null;

                        var clientDetail = clientNamesMapForPdf?.FirstOrDefault(c => c.UserId == clientId && c.Address != null);
                        if (clientDetail != null)
                        {
                            var guarantorContact = clientDetail.Name;

                            if (guarantorContact == null)
                                continue;

                            var fullName = $"{guarantorContact?.FirstName} {guarantorContact?.MiddleName} {guarantorContact?.LastName}";
                            var contact = string.IsNullOrWhiteSpace(clientDetail.PhoneNumber) && string.IsNullOrWhiteSpace(clientDetail.Email) ? ""
                                          : $"{(string.IsNullOrWhiteSpace(clientDetail.PhoneNumber) ? "-" : clientDetail.PhoneNumber)} / {(string.IsNullOrWhiteSpace(clientDetail.Email) ? "-" : clientDetail.Email)}";

                            var address = $"{clientDetail?.Address?.Street1} {clientDetail?.Address?.Street2} {clientDetail?.Address?.City} {clientDetail?.Address?.State} {clientDetail?.Address?.ZipCode} {clientDetail?.Address?.Country}";

                            guarantor = new GuarantorInfo()
                            {
                                Name = fullName,
                                Contact = contact,
                                Address = address
                            };

                        }
                        else
                        {
                            guarantor = new GuarantorInfo()
                            {
                                Name = "",
                                Address = ""
                            };
                        }

                        foreach (var invoice in invoices)
                        {
                            invoice.BillingProviderInfo = billingProviderInfo;
                            invoice.clientInfo = clientInfo;
                            invoice.guarantorInfo = guarantor;
                            invoice.Message = _invoiceMessage;
                            invoice.Remark = _invoiceRemark;
                        }
                        var template = await _razorViewService.RenderViewToStringAsync("PatientInvoiceView", invoices);
                        combinedHtml.Append(template);
                    }
                    catch (Exception ex)
                    {
                        errorList.Add($"Error processing client {clientId}: {ex.Message}");
                    }
                }

                if (errorList.Any() && combinedHtml.Length == 0)
                {
                    return (null, errorList);
                }

                var html = combinedHtml.ToString();
                var pdfData = await _PdfService.GeneratePDF(html);
                return (pdfData, errorList);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate PDF invoices: {ex.Message}", ex);
            }
        }

        public async Task<PatientInvoiceViewModel> GenerateInvoice(int accountId, int clientId, List<ChargeModel> charges)
        {
            string invoiceNumber = GenerateInvoiceNumber(Convert.ToString(clientId));
            // add the adjustmentPatientResponsibility
            var totalAdjustmentPatientResponsibility = charges.Sum(c => c.AdjustmentPatientResponsibility );
            var totalPatientPayments = charges.Sum(c => c.PatientPayments );
            var totalPatientBalance = charges.Sum(c => c.PatientBalance);

            PatientInvoiceStatus newStatus = PatientInvoiceStatus.InvoiceSent;

            // Determine invoice status based on aggregated values
            if (totalPatientBalance <= 0)
            {
                newStatus = PatientInvoiceStatus.FullyPaid;
            }
            else if (totalAdjustmentPatientResponsibility > 0 && totalPatientPayments >0)
            {
                newStatus = PatientInvoiceStatus.PartiallyPaid;
            }

            var invoice = new PatientInvoiceEntity
            {
                AccountId = accountId,
                ClientId = clientId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = EstDateTime,
                PaymentDueDate = GetPaymentDueDate(),
                Status = newStatus,
                TotalAmount = charges.Sum(c => c.PatientBalance),
                DateCreated = EstDateTime,
                DateLastModified = EstDateTime
            };

            _patientInvoiceRepository.Add(invoice);
            await _patientInvoiceRepository.SaveChangesAsync();

            await SaveGuarantorSnapshotAsync(invoice.Id, accountId, clientId);

            var chargeIds = charges.Select(c => c.ChargeId).Distinct().ToList();
            var chargeEntries = await getChargeDetails(chargeIds);
            var chargeEntryLookup = chargeEntries.ToDictionary(c => c.Id);
            var groupByChargeData = await _paymentClaimService.GetGroupedByPaymentsForPatientInvoice(chargeIds);

            var invoiceDetails = charges.Select(c =>
            {
                var chargeDetail = chargeEntryLookup.GetValueOrDefault(c.ChargeId);
                var chargeAmounts = groupByChargeData.FirstOrDefault(x => x.ChargeId == c.ChargeId);
                return new PatientInvoiceDetailsEntity
                {
                    InvoiceId = invoice.Id,
                    ChargeId = c.ChargeId,
                    BilledAmount = chargeDetail?.BilledAmount ?? 0,
                    InsurancePayments = chargeAmounts?.InsurancePayment ?? 0,
                    PatientPayments = chargeAmounts?.PatientPayment ?? 0,
                    AdjustmentNonPatientResponsibility = (chargeAmounts?.Adjustment ?? 0) + (chargeDetail?.WriteOffAmount ?? 0),
                    AdjustmentPatientResponsibility = chargeAmounts?.PatientResponsibility ?? 0,
                    PatientBalance = chargeAmounts?.PatientResponsibilityBalance ?? 0,
                    DateCreated = EstDateTime,
                    DateLastModified = EstDateTime
                };
            }).ToList();

            await _patientInvoiceDetailsRepository.AddRangeAsync(invoiceDetails);
            await _patientInvoiceDetailsRepository.SaveChangesAsync();

            var billingDetails = charges.Select(c =>
            {
                var chargeEntry = chargeEntryLookup.GetValueOrDefault(c.ChargeId);
                var chargeAmounts = groupByChargeData.FirstOrDefault(x => x.ChargeId == c.ChargeId);

                return new BillingDetailViewModel
                {
                    BillingCode = chargeEntry?.BillingCode,
                    Units = Math.Round(chargeEntry?.Units ?? 0, 2),
                    DateOfService = chargeEntry?.DateOfService.ToString("MM/dd/yyyy"),
                    BilledAmount = Math.Round(chargeEntry?.BilledAmount ?? 0, 2),
                    InsurancePayments = Math.Round(chargeAmounts?.InsurancePayment ?? 0, 2),
                    PatientPayments = Math.Round(chargeAmounts?.PatientPayment ?? 0, 2),
                    Adjustments = Math.Round((chargeAmounts?.Adjustment ?? 0) + (chargeEntry?.WriteOffAmount ?? 0), 2),
                    AdjustmentsPR = Math.Round(chargeAmounts?.PatientResponsibility ?? 0, 2),
                    PatientBalance = Math.Round(chargeAmounts?.PatientResponsibilityBalance ?? 0, 2)
                };
            }).ToList();

            return new PatientInvoiceViewModel
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = EstDateTime.ToString("MM/dd/yyyy"),
                PaymentDue = GetPaymentDueDate().ToString("MM/dd/yyyy"),
                BillingDetails = billingDetails,
                IsPreviousInvoice = false
            };
        }

        private async Task SaveGuarantorSnapshotAsync(int invoiceId, int accountId, int clientId)
        {
            try
            {
                var exists = await _patientGuarantorRepository
                    .Query()
                    .AnyAsync(g => g.InvoiceId == invoiceId && g.DeletedOn == null);

                if (exists) return;

                var guarantorInfo = await _rethinkServices.GetContactGuarantorDetails(accountId, clientId);
                if (guarantorInfo == null)
                    return;

                var entity = new PatientGuarantorEntity
                {
                    InvoiceId = invoiceId,
                    GuarantorId = guarantorInfo.Id,
                    ClientId = clientId,
                    AccountId = accountId,
                    UserType = guarantorInfo.UserType,
                    IsPrimaryContact = guarantorInfo.IsPrimaryContact,
                    IsGuarantor = guarantorInfo.IsGuarantor,
                    FirstName = guarantorInfo.Name?.firstName,
                    MiddleName = guarantorInfo.Name?.middleName,
                    LastName = guarantorInfo.Name?.lastName,
                    Prefix = guarantorInfo.Name?.prefix,
                    Suffix = guarantorInfo.Name?.suffix,
                    Email = guarantorInfo.Email,
                    Phone = guarantorInfo.PhoneNumber,
                    RelationToClient = guarantorInfo.RelationToClient,
                    RelationshipToInsured = guarantorInfo.RelationshipToInsured,
                    GenderId = guarantorInfo.GenderId,
                    MaritalStatusId = guarantorInfo.MaritalStatusId,
                    DateOfBirth = guarantorInfo.DateOfBirth,
                    TimezoneId = guarantorInfo.TimezoneId,
                    AddressId = guarantorInfo.Address?.Id ?? 0,
                    Street1 = guarantorInfo.Address?.street1,
                    Street2 = guarantorInfo.Address?.street2,
                    City = guarantorInfo.Address?.city,
                    StateId = guarantorInfo.Address?.stateId,
                    ZipCode = guarantorInfo.Address?.zipCode,
                    CountryId = guarantorInfo.Address?.countryId,
                    Town = guarantorInfo.Address?.town,
                    CreatedOn = guarantorInfo.MetaData?.createdOn ?? EstDateTime,
                    CreatedBy = guarantorInfo.MetaData?.createdBy,
                    ModifiedOn = guarantorInfo.MetaData?.modifiedOn,
                    ModifiedBy = guarantorInfo.MetaData?.modifiedBy,
                    DeletedOn = guarantorInfo.MetaData?.deletedOn,
                    DeletedBy = guarantorInfo.MetaData?.deletedBy
                };

                _patientGuarantorRepository.Add(entity);
                await _patientGuarantorRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveGuarantorSnapshotAsync failed: " + ex.Message);
            }
        }

        public async Task<List<PatientInvoiceViewModel>> GetPreviousInvoices(int accountId, int clientId, string invoiceNo)
        {
            var previousInvoices = new List<PatientInvoiceEntity>();
            if (invoiceNo != null)
            {
                previousInvoices = await _patientInvoiceRepository
                .Query()
                .Where(i => i.InvoiceNumber == invoiceNo && i.DateDeleted == null)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
            }
            else
            {
                previousInvoices = await _patientInvoiceRepository
                .Query()
                .Where(i => i.AccountId == accountId && i.ClientId == clientId && i.DateDeleted == null)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
            }

            var invoiceIds = previousInvoices.Select(i => i.Id).ToList();
            var invoiceDetailsTask = _patientInvoiceDetailsRepository.Query()
                .Where(d => invoiceIds.Contains(d.InvoiceId) && d.DateDeleted == null)
                .ToListAsync();
            var chargeEntriesTask = getChargeDetails(invoiceDetailsTask.Result.Select(d => d.ChargeId).Distinct().ToList());

            var invoiceDetails = await invoiceDetailsTask;
            var chargeEntries = await chargeEntriesTask;

            var chargeEntryLookup = chargeEntries.ToDictionary(c => c.Id);
            var groupByChargeData = await _paymentClaimService.GetGroupedByPaymentsForPatientInvoice(invoiceDetailsTask.Result.Select(d => d.ChargeId).Distinct().ToList());

            var previousInvoicesViewModel = previousInvoices.Select(previousInvoice =>
            {
                var details = invoiceDetails.Where(d => d.InvoiceId == previousInvoice.Id).ToList();
                var billingDetails = details.Select(d =>
                {
                    var chargeEntry = chargeEntryLookup.GetValueOrDefault(d.ChargeId);
                    var chargeAmounts = groupByChargeData.FirstOrDefault(x => x.ChargeId == d.ChargeId);
                    return new BillingDetailViewModel
                    {
                        BillingCode = chargeEntry?.BillingCode,
                        Units = Math.Round(chargeEntry?.Units ?? 0, 2),
                        DateOfService = chargeEntry.DateOfService.ToString("MM/dd/yyyy"),
                        BilledAmount = Math.Round(chargeEntry?.BilledAmount ?? 0, 2),
                        InsurancePayments = Math.Round(chargeAmounts?.InsurancePayment ?? 0, 2),
                        PatientPayments = Math.Round(chargeAmounts?.PatientPayment ?? 0, 2),
                        Adjustments = Math.Round((chargeAmounts?.Adjustment ?? 0) + (chargeEntry?.WriteOffAmount ?? 0), 2),
                        AdjustmentsPR = Math.Round(chargeAmounts?.PatientResponsibility ?? 0, 2),
                        PatientBalance = Math.Round(chargeAmounts?.PatientResponsibilityBalance ?? 0, 2)
                    };
                }).ToList();

                return new PatientInvoiceViewModel
                {
                    InvoiceNumber = previousInvoice.InvoiceNumber,
                    InvoiceDate = previousInvoice.InvoiceDate.ToString("MM/dd/yyyy"),
                    PaymentDue = previousInvoice.PaymentDueDate.ToString("MM/dd/yyyy"),
                    BillingDetails = billingDetails,
                    IsPreviousInvoice = true
                };
            }).ToList();

            return previousInvoicesViewModel;
        }

        public async Task<(List<InvoiceDetailsModel> Data, List<ClaimFilterOptionModel> UserList, int TotalCount)>GetInvoiceDetails(PendingCollectionFilters filter)
        {
            _logger.LogInformation("GetInvoiceDetails started. AccountInfoId={AccountInfoId}", filter.AccountInfoId);
            var invoicesQuery = _patientInvoiceRepository
                .Query()
                .AsNoTracking()
                .Where(i => i.AccountId == filter.AccountInfoId && i.DateDeleted == null);

            if (filter.Filters != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Filters.ClientIds))
                {
                    var clientIds = filter.Filters.ClientIds.Split(',')
                        .Select(int.Parse)
                        .ToList();

                    invoicesQuery = invoicesQuery.Where(i => clientIds.Contains(i.ClientId));
                }

                if (filter.Filters.InvoiceFrom.HasValue)
                    invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate >= filter.Filters.InvoiceFrom.Value);

                if (filter.Filters.InvoiceTo.HasValue)
                    invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate <= filter.Filters.InvoiceTo.Value);

                if (filter.Filters.PaymentDueFrom.HasValue)
                    invoicesQuery = invoicesQuery.Where(i => i.PaymentDueDate >= filter.Filters.PaymentDueFrom.Value);

                if (filter.Filters.PaymentDueTo.HasValue)
                    invoicesQuery = invoicesQuery.Where(i => i.PaymentDueDate <= filter.Filters.PaymentDueTo.Value);
            }

            var invoices = await invoicesQuery
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();

            if (!invoices.Any())
                return ([], [], 0);

            var invoiceIds = invoices.Select(i => i.Id).ToHashSet();
            var clientIdsDistinct = invoices.Select(i => i.ClientId).Distinct().ToList();

            var clientUsersTask = _rethinkServices.GetChildProfilesForAccount(filter.AccountInfoId);

            var clientDetailsTask = _claimsSearchClientsRepository
                .Query()
                .AsNoTracking()
                .Where(c => clientIdsDistinct.Contains(c.Id))
                .Select(c => new ClientDetail
                {
                    Id = c.Id,
                    ClientName =
                        (c.firstName ?? "") +
                        (string.IsNullOrWhiteSpace(c.middleName) ? "" : " " + c.middleName.Trim()) +
                        (string.IsNullOrWhiteSpace(c.lastName) ? "" : " " + c.lastName)
                })
                .ToListAsync();

            await Task.WhenAll(clientUsersTask, clientDetailsTask);

            var invoiceDetails = await _patientInvoiceDetailsRepository
                .Query()
                .AsNoTracking()
                .Where(d => invoiceIds.Contains(d.InvoiceId) && d.DateDeleted == null)
                .ToListAsync();


            var chargeIds = invoiceDetails.Select(d => d.ChargeId).Distinct().ToList();

            var chargeEntries = await getChargeDetails(chargeIds);
            var chargeLookup = chargeEntries.ToDictionary(c => c.Id);

            var paymentsLookup = (await _paymentClaimService
                .GetGroupedByPaymentsForPatientInvoice(chargeIds))
                .ToDictionary(x => x.ChargeId);

            var clientLookup = clientDetailsTask.Result
                .ToDictionary(c => c.Id, c => c.ClientName);

            foreach (var u in clientUsersTask.Result)
            {
                if (!clientLookup.ContainsKey(u.Id))
                {
                    clientLookup[u.Id] =
                        $"{u.FirstName} {u.MiddleName ?? ""} {u.LastName ?? ""}".Trim();
                }
            }

            var invoiceDetailsLookup = invoiceDetails
                .GroupBy(d => d.InvoiceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var allBillingDetails = new List<BillingDetailViewModel>();

            var invoiceModels = invoices.Select(inv =>
            {
                if (!invoiceDetailsLookup.TryGetValue(inv.Id, out var details))
                    return null;

                var billingDetails = details.Select(d =>
                {
                    chargeLookup.TryGetValue(d.ChargeId, out var charge);
                    paymentsLookup.TryGetValue(d.ChargeId, out var pay);

                    return new BillingDetailViewModel
                    {
                        Id = charge?.Id ?? 0,
                        ClientId = inv.ClientId,
                        BillingCode = charge?.BillingCode ?? "",
                        BilledAmount = Math.Round(charge?.BilledAmount ?? 0, 2),
                        Adjustments = Math.Round((pay?.Adjustment ?? 0) + (charge?.WriteOffAmount ?? 0), 2),
                        AdjustmentsPR = Math.Round(pay?.PatientResponsibility ?? 0, 2),
                        InsurancePayments = Math.Round(pay?.InsurancePayment ?? 0, 2),
                        PatientPayments = Math.Round(pay?.PatientPayment ?? 0, 2),
                        PatientBalance = Math.Round(pay?.PatientResponsibilityBalance ?? 0, 2),
                        Units = charge?.Units ?? 0,
                        DateOfService = charge?.DateOfService.ToString("MM/dd/yyyy"),
                        InvoiceNumber = inv.InvoiceNumber,
                        InvoiceDate = inv.InvoiceDate.ToString("MM/dd/yyyy"),
                        PaymentDue = inv.PaymentDueDate.ToString("MM/dd/yyyy"),
                        Status = GetEnumDescription(inv.Status)
                    };
                }).ToList();

                if (filter.Filters?.PatientResponsibilityFrom.HasValue == true ||
                    filter.Filters?.PatientResponsibilityTo.HasValue == true)
                {
                    billingDetails = billingDetails.Where(b =>
                        (!filter.Filters.PatientResponsibilityFrom.HasValue || b.AdjustmentsPR >= filter.Filters.PatientResponsibilityFrom.Value) &&
                        (!filter.Filters.PatientResponsibilityTo.HasValue || b.AdjustmentsPR <= filter.Filters.PatientResponsibilityTo.Value))
                        .ToList();
                }

                allBillingDetails.AddRange(billingDetails);

                return new InvoiceDetailsModel
                {
                    Id = inv.ClientId,
                    ClientName = clientLookup.GetValueOrDefault(inv.ClientId, "Unknown"),
                    TotalBilledAmount = billingDetails.Sum(x => x.BilledAmount),
                    TotalAdjustments = billingDetails.Sum(x => x.Adjustments),
                    TotalAdjustmentsPR = billingDetails.Sum(x => x.AdjustmentsPR),
                    TotalInsurancePayments = billingDetails.Sum(x => x.InsurancePayments),
                    TotalPatientPayments = billingDetails.Sum(x => x.PatientPayments),
                    TotalPatientBalance = billingDetails.Sum(x => x.PatientBalance),
                    BillingDetails = billingDetails
                };
            })
            .Where(x => x != null)
            .ToList();

            var guarantorMap = await _rethinkServices
                .GetClientDetailsGuarantor(filter.AccountInfoId);

            var guarantorDict = guarantorMap?
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var inv in invoiceModels)
            {
                RethinkGuarantorDetails.ClientModel g = null;
                var hasGuarantor = guarantorDict != null &&
                    guarantorDict.TryGetValue(inv.Id, out g) &&
                    g?.Address != null;
                inv.GuarantorName = hasGuarantor
                    ? $"{g.Name.FirstName} {g.Name.MiddleName} {g.Name.LastName}"
                    : "Missing Guarantor";
            }

            var billingDetailsByClient = allBillingDetails
                .GroupBy(b => b.ClientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = invoiceModels
                .GroupBy(x => x.Id)
                .Select(g => new InvoiceDetailsModel
                {
                    Id = g.Key,
                    ClientName = g.First().ClientName,
                    TotalBilledAmount = g.Sum(x => x.TotalBilledAmount),
                    TotalAdjustments = g.Sum(x => x.TotalAdjustments),
                    TotalAdjustmentsPR = g.Sum(x => x.TotalAdjustmentsPR),
                    TotalInsurancePayments = g.Sum(x => x.TotalInsurancePayments),
                    TotalPatientPayments = g.Sum(x => x.TotalPatientPayments),
                    TotalPatientBalance = g.Sum(x => x.TotalPatientBalance),
                    BillingDetails = billingDetailsByClient.TryGetValue(g.Key, out var details) ? details : new List<BillingDetailViewModel>(),
                    GuarantorName = g.First().GuarantorName
                })
                .ToList();

            var userList = result
                .Select(x => new ClaimFilterOptionModel { Id = x.Id, Name = x.ClientName })
                .ToList();

            var totalCount = result.Count;

            var data = filter.Take == 0
                ? result
                : result.Skip(filter.Skip).Take(filter.Take).ToList();

            return (data, userList, totalCount);
        }

        public async Task<(byte[] pdfData, List<string> ErrorList)> GetInvoicePDF(int accountId, int clientId, string InvoiceNo)
        {
            var combinedHtml = new StringBuilder();
            var errorList = new List<string>();

            try
            {
                var invoices = new List<PatientInvoiceViewModel>();
                try
                {
                    var invoiceId = _patientInvoiceRepository.Query().FirstOrDefault(x => x.InvoiceNumber == InvoiceNo)?.Id ?? 0;

                    var clientDetail = _patientGuarantorRepository.Query().FirstOrDefault
                        (x => x.InvoiceId == invoiceId);
                    GuarantorInfo guarantor = null;

                    if (clientDetail != null)
                    {
                        var fullName = $"{clientDetail?.FirstName} {clientDetail?.MiddleName} {clientDetail?.LastName}";
                        var contact = string.IsNullOrWhiteSpace(clientDetail.Phone) && string.IsNullOrWhiteSpace(clientDetail.Email) ? ""
                                      : $"{(string.IsNullOrWhiteSpace(clientDetail.Phone) ? "-" : clientDetail.Phone)} / {(string.IsNullOrWhiteSpace(clientDetail.Email) ? "-" : clientDetail.Email)}";
                        var address = $"{clientDetail?.Street1} {clientDetail?.Street2} {clientDetail?.City} {clientDetail?.State} {clientDetail?.ZipCode} {clientDetail?.Country}";

                        guarantor = new GuarantorInfo()
                        {
                            Name = fullName,
                            Contact = contact,
                            Address = address
                        };
                    }
                    else
                    {
                        guarantor = new GuarantorInfo()
                        {
                            Name = "",
                            Address = ""
                        };
                    }

                    var (clientInfo, billingProviderInfo) = await GetClientBillingProviderInfoFromCache(accountId, clientId);

                    var previousInvoices = await GetPreviousInvoices(accountId, clientId, InvoiceNo);
                    invoices.AddRange(previousInvoices);

                    foreach (var invoice in invoices)
                    {
                        invoice.BillingProviderInfo = billingProviderInfo;
                        invoice.clientInfo = clientInfo;
                        invoice.guarantorInfo = guarantor;
                        invoice.Message = _invoiceMessage;
                        invoice.Remark = _invoiceRemark;
                    }
                    var template = await _razorViewService.RenderViewToStringAsync("PatientInvoiceView", invoices);
                    combinedHtml.Append(template);
                }
                catch (Exception ex)
                {
                    errorList.Add($"Error processing client {clientId}: {ex.Message}");
                }

                if (errorList.Any() && combinedHtml.Length == 0)
                {
                    return (null, errorList);
                }
                var html = combinedHtml.ToString();
                var pdfData = await _PdfService.GeneratePDF(html);
                return (pdfData, errorList);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate PDF invoices: {ex.Message}", ex);
            }
        }

        private string GenerateInvoiceNumber(string clientId)
        {
            if (clientId.Length > 10) clientId = clientId.Substring(0, 10);
            string invoiceDate = EstDateTime.ToString("yyyyMMdd");
            int sequenceNumber = GetNextSequenceNumberForAccount(clientId);
            string sequenceFormatted = sequenceNumber.ToString("D3");
            return $"INV-{clientId}-{invoiceDate}-{sequenceFormatted}";
        }

        private async Task<(Templates.ViewModels.ClientInfo, BillingProviderInfo)> GetClientBillingProviderInfoFromCache(int accountId, int clientId)
        {
            var cacheKeyClientInfo = $"ClientInfo_{accountId}_{clientId}";
            var cacheKeyBillingInfo = $"BillingProviderInfo_{accountId}_{clientId}";

            var clientInfo = await _CacheService.GetOrSetCacheAsync(
                cacheKeyClientInfo,
                () => _clientInfoService.GetClientInfo(accountId, clientId),
                TimeSpan.FromMinutes(_cacheExpiration));

            var billingProviderInfo = await _cacheManager.GetAsync(
                    cacheKeyBillingInfo,
                    async () => await _clientInfoService.GetBillingProviderInfo(accountId, clientId),
                    CachingDuration.TenMinutes
                );

            return (clientInfo, billingProviderInfo);
        }

        private async Task<List<ChargeDetails>> getChargeDetails(List<int> chargeEntryIds)
        {
            _logger.LogInformation("getChargeDetails started :");
           
                var result =
                    await _chargeEntryRepository
                        .Query()
                        .AsNoTracking()
                        .Where(c => chargeEntryIds.Contains(c.Id))
                        .Select(c => new ChargeDetails
                        {
                            Id = c.Id,
                            BillingCode = c.BillingCode,
                            Units = c.Units,
                            DateOfService = c.DateOfService,
                            BilledAmount = c.Charges,
                            WriteOffAmount =
                                _claimChargeEntryWriteOffEntity.Query()
                                    .Where(w =>
                                        w.ClaimChargeEntryId == c.Id &&
                                        w.DateDeleted == null)
                                    .Sum(w => (decimal?)w.WriteOffAmount) ?? 0m
                        })
                        .ToListAsync();

                _logger.LogInformation("getChargeDetails ended :");
                return result;           
        }

        private int GetNextSequenceNumberForAccount(string clientId)
        {
            try
            {
                var lastInvoice = _patientInvoiceRepository.Query()
                    .Where(i => i.ClientId == Convert.ToInt32(clientId) && i.InvoiceDate.Date == EstDateTime.Date)
                    .OrderByDescending(i => i.InvoiceNumber)
                    .FirstOrDefault();

                if (lastInvoice != null)
                {
                    var lastSequence = int.Parse(lastInvoice.InvoiceNumber.Split('-').Last());
                    return lastSequence + 1;

                }
                else
                {
                    return 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching previos invoice number : " + ex.Message.ToString());
                return -1;
            }
        }

        private DateTime GetPaymentDueDate()
        {
            string paymentDuedate = EstDateTime.AddDays(30).ToString("yyyyMMdd");
            DateTime dueDate = DateTime.ParseExact(paymentDuedate, "yyyyMMdd", null);
            return dueDate;
        }
    }
}
