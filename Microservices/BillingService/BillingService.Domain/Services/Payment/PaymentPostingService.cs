using AutoMapper;
using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Configuration;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Extensions;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Thon.Hotels.FishBus;

namespace BillingService.Domain.Services.Payment
{
    public class PaymentPostingService : BaseService, IPaymentPostingService
    {
        private const string guarantorDetailsCodeKey = "gurantorDetails";
        private const string accountChildCodeKey = "accountChildDetails";
        private const int chunkSize = 100;

        private readonly IRepository<BillingDbContext, PaymentEntity> _paymentRepository;
        private readonly IRepository<BillingDbContext, PaymentEraUploadEntity> _paymentEraUploadRepository; 
        private readonly IPaymentMethodService _paymentMethodService;
        private readonly IBlobProcessingService _blobProcessingService;
        private readonly IBillingBlobService _billingBlobService;
        private readonly IServiceBusConnectionFactory _serviceBusConnectionFactory;
        private readonly IMapper _mapper;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IChargeEntryService _chargeEntryService;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> _paymentClaimServiceLineAdjustmentRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IMessageBus _bus;       
        private readonly IBillingFilePath _billingFilePath;      
        private readonly IRepository<BillingDbContext, UnAllocatedPaymentEntity> _unAllocatedPaymentRepository;
        private readonly IConfiguration _configuration;
        private readonly ICHService _blobBackupService;
        private readonly ICacheService _cacheService;
        
        private static string _manualUploadBlobContainerName = "eramanualupload";
        private int cacheExpiration = 10;

        public PaymentPostingService(
            IRepository<BillingDbContext, PaymentEntity> paymentRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IPaymentMethodService paymentMethodService,
            IMapper mapper,
            IRepository<BillingDbContext, PaymentEraUploadEntity> paymentEraUploadRepository,
            IBlobProcessingService blobProcessingService,
            IBillingBlobService billingBlobService,
            IServiceBusConnectionFactory serviceBusConnectionFactory,
            IClaimHistoryService claimHistoryService,
            IChargeEntryService chargeEntryService,
            IClaimManagerService claimManagerService,
            IRethinkMasterDataMicroServices rethinkServices,
            IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustmentRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IMessageBus bus,
            IBillingFilePath billingFilePath,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, UnAllocatedPaymentEntity> unAllocatedPaymentRepository,
            IConfiguration configuration,
            ICHService blobBackupService,
            ICacheService cacheService
            )
        {
            _paymentRepository = paymentRepository;
            _paymentClaimRepository = paymentClaimRepository;
            _paymentMethodService = paymentMethodService;
            _mapper = mapper;
            _paymentEraUploadRepository = paymentEraUploadRepository;
            _blobProcessingService = blobProcessingService;
            _billingBlobService = billingBlobService;
            _serviceBusConnectionFactory = serviceBusConnectionFactory;
            _claimHistoryService = claimHistoryService;
            _chargeEntryService = chargeEntryService;
            _claimManagerService = claimManagerService;
            _rethinkServices = rethinkServices;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _paymentClaimServiceLineAdjustmentRepository = paymentClaimServiceLineAdjustmentRepository;
            _bus = bus;
            _billingFilePath = billingFilePath;
            _claimRepository = claimRepository;
            _unAllocatedPaymentRepository = unAllocatedPaymentRepository;
            _configuration = configuration;
            _blobBackupService = blobBackupService;
            _cacheService = cacheService;
        }

        public async Task<PaymentsResponseModel> GetAllPayments(GetPaymentsModel getPaymentsModel)
        {
            if (getPaymentsModel.SortingModels == null || getPaymentsModel.SortingModels.Count == 0)
            {
                getPaymentsModel.SortingModels = new List<SortingModel>
                {
                    new SortingModel
                    {
                        Dir = "desc",
                        Field = "ReceivedDate"
                    }
                };
            }

            foreach (var model in getPaymentsModel.FilterModels.Where(x => x.PropertyName.ToLower() == "ismanual").ToList())
            {
                switch (model.Value.ToLower())
                {
                    case "manual":
                        model.Value = "true";
                        break;

                    case "electronic":
                        model.Value = "false";
                        break;

                    default:
                        getPaymentsModel.FilterModels.Remove(model);
                        break;
                }
            }

            var dataQuery = _paymentRepository.Query().Include(x => x.PaymentClaims)
                    .Where(x => x.AccountInfoId == getPaymentsModel.AccountInfoId &&
                                (x.IsManualPayment == true || (x.IsManualPayment == false && (x.EraDocumentEdi != null || (PaymentMethods)x.PaymentMethodId == PaymentMethods.RevSpring))) &&
                                x.DateDeleted == null)
                .Select(x => new PaymentModel
                {
                    Id = x.Id,
                    FunderName = string.IsNullOrEmpty(x.FunderName) ? x.PaymentTypeEntity != null ?
                            string.IsNullOrEmpty(x.PaymentTypeEntity.Description) ? x.PaymentTypeEntity.Name
                                : x.PaymentTypeEntity.Description : ""
                            : x.FunderName,
                    FunderId = x.FunderID,
                    ClaimIds = x.PaymentClaims.Where(y => y.DateDeleted == null && y.ClaimId != null).Select(y => y.ClaimId.Value).ToList(),
                    PaymentAmount = x.PaymentAmount,
                    EraPaymentMethod = x.EraPaymentMethod != null ? x.EraPaymentMethod : "",
                    PaymentMethodId = x.PaymentMethodId,
                    PaymentMethodName=  x.PaymentMethodId == (int)PaymentMethods.Cash ? "Cash" :
                                        x.PaymentMethodId == (int)PaymentMethods.Check ? "Check" :
                                        x.PaymentMethodId == (int)PaymentMethods.ACH ? "ACH" :
                                        x.PaymentMethodId == (int)PaymentMethods.Transfer ? "Transfer" :
                                        x.PaymentMethodId == (int)PaymentMethods.CreditCard ? "Credit Card" :
                                        x.PaymentMethodId == (int)PaymentMethods.NonPayment ? "Non-Payment" :
                                        x.PaymentMethodId == (int)PaymentMethods.FSAHSA ? "FSA/HSA" :               
                                        x.PaymentMethodId == (int)PaymentMethods.RevSpring ? "RevSpring" :
                                        "",
                    ReceivedDate = x.ReceivedDate,
                    ClaimsCount = x.PaymentClaims.Where(x => x.DateDeleted == null).Count(),
                    Reference = x.ReferenceNumber != null ? x.ReferenceNumber : "",
                    PaymentIdentifier = x.PaymentIdentifier,
                    DeniedClaimsCount =
                            x.PaymentClaims.Where(x => x.DateDeleted == null).Count(c => c.ClaimStatus == ((int)PaymentClaimStatus.Denied).ToString()),
                    AppliedAmount = x.PaymentClaims.Where(y => y.DateDeleted == null).Sum(c => c.TotalPayment) ?? 0,
                    ReconcileStatus = x.IsManualReconciled ? "Fully"
                        : (x.PaymentClaims.Where(x => x.DateDeleted == null).Sum(c => c.TotalPayment) ?? 0) == 0 ? "None"
                        : (x.PaymentClaims.Where(x => x.DateDeleted == null).Sum(c => c.TotalPayment) ?? 0) >= x.PaymentAmount ? "Fully"
                        : "Partially",
                    IsManual = x.IsManualPayment,
                    PaymentType = (PaymentTypes)x.PaymentTypeId,
                    PaymentChannel = (x.IsManualPayment ? PaymentChannel.Manual : PaymentChannel.Electronic).ToString(),
                    PaymentSource = !x.IsManualPayment
                        ? (x.PaymentMethodId == (int)PaymentMethods.RevSpring
                            ? PaymentSource.RevSpring
                            : PaymentSource.Payer).ToString()
                        : null,
                }).OrderBy(getPaymentsModel.SortingModels)
                .Filter(getPaymentsModel.FilterModels);

            var selectQuery = getPaymentsModel.Take == 0 ? dataQuery.Skip(getPaymentsModel.Skip).ToList() : dataQuery.Skip(getPaymentsModel.Skip).Take(getPaymentsModel.Take).ToList();

            // Calculate DeniedClaimsCount if not already set
            var claimIds = selectQuery.SelectMany(x => x.ClaimIds).Distinct().ToList();

            if (claimIds.Count > 0)
            {
                var claims = await _claimRepository.Query()
                    .Where(x => claimIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        x.ClaimStatus
                    }).ToListAsync();

                var deniedClaimIds = claims
                    .Where(c => c.ClaimStatus == ClaimStatus.Denied)
                    .Select(c => c.Id)
                    .ToHashSet();

                // Update the selectQuery with the claim statuses
                foreach (var payment in selectQuery)
                {
                    if (payment.DeniedClaimsCount == 0)
                    {
                        payment.DeniedClaimsCount = payment.ClaimIds.Count(id => deniedClaimIds.Contains(id));
                    }
                    payment.ClaimIds = [];
                }
            }

            var result = selectQuery.ToList();

            var totalCount = await dataQuery.CountAsync();

            var response = new PaymentsResponseModel
            {
                Data = result,
                TotalCount = totalCount
            };

            var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(getPaymentsModel.AccountInfoId, false);
            response.isRevSpringEnabled = accountInfo.subscriptionFeatures != null
                                 && accountInfo.subscriptionFeatures.ContainsKey("showRevSpring")
                                 && (bool)accountInfo.subscriptionFeatures["showRevSpring"];

            return response;
        }

        public async Task<FunderDropdownResponseModel> GetAssignedFundersAsync(FunderSearchModelWithUserInfo funderSearchModel)
        {
            //Create key base on the accountId
            string accountFunderListKey = $"Funder-List-For-{funderSearchModel.AccountInfoId}";
            var funderList = await _cacheService.GetOrSetCacheAsync(
                accountFunderListKey,
                 async () => await _rethinkServices.GetAllFundersForAccount(funderSearchModel.AccountInfoId),
                TimeSpan.FromMinutes(cacheExpiration)
            );

            var result = funderList
                .Where(x => (string.IsNullOrEmpty(funderSearchModel.FunderName) ||
                            x.funderName.ToLower().StartsWith(funderSearchModel.FunderName.ToLower())
                            && x.isActive))
                .Skip(funderSearchModel.Skip)
                .Take(funderSearchModel.Take)
                .Select(x => new FunderDropdownModel
                {
                    Id = x.id,
                    FunderName = x.funderName
                }).ToList();

            var response = new FunderDropdownResponseModel
            {
                Funders = result,
                TotalCount = result.Count()
            };

            return response;
        }

        public List<PaymentMethodsModel> GetPaymentMethods()
        {
            var result = Enum.GetNames(typeof(PaymentMethods)).Select(x =>
            {
                Enum.TryParse(typeof(PaymentMethods), x, out var enumValue);

                var displayName =
                    typeof(PaymentMethods)
                        .GetMember(enumValue.ToString())
                        .First()
                        .GetCustomAttribute<DescriptionAttribute>()?
                        .Description ?? x;

                return new PaymentMethodsModel
                {
                    EnumValue = (int)enumValue,
                    DisplayName = displayName,
                };
            }).ToList();

            return result;
        }

        public List<string> GetReconcileStatuses()
        {
            return Enum.GetNames(typeof(ReconcileStatuses)).ToList();
        }

        public async Task<PaymentSummary> GetPaymentSummaryAsync(int paymentId)
        {
            var result = await _paymentRepository.Query()
                .Where(p => p.Id == paymentId && p.DateDeleted == null)
                .Include(x => x.PaymentClaims)
                .Select(p => new PaymentSummary
                {
                    Id = p.Id,
                    PaymentAmount = p.PaymentAmount,
                    PostedAmount = p.PaymentClaims.Where(x => x.DateDeleted == null).Sum(x => x.TotalPayment ?? 0),
                    RemainingAmount = p.PaymentAmount - p.PaymentClaims.Where(x => x.DateDeleted == null).Sum(x => x.TotalPayment ?? 0),
                    Payee = p.Payee,
                    FunderName = p.FunderName,
                    PaymentMethod = p.EraPaymentMethod ?? p.PaymentMethodEntity.Name,
                    PaymentMethodId = p.PaymentMethodId,
                    PostDate = p.PostDate,
                    ReferenceNumber = p.ReferenceNumber,
                    DepositDate = p.DepositDate,
                    IsManual = p.IsManualPayment,
                    PaymentTypeId = p.PaymentTypeId,
                }).FirstOrDefaultAsync();

            return result;
        }

        public async Task UpdateManualPaymentSummaryAsync(UpdateManualPaymentSummary model)
        {
            var payment = await _paymentRepository.GetByIdAsync(model.Id);

            payment.DepositDate = model.DepositDate;
            payment.PaymentMethodId = model.PaymentMethodId;
            payment.ReferenceNumber = model.ReferenceNumber;
            payment.PostDate = model.PostDate;
            payment.PaymentAmount = model.PaymentAmount;

            MarkUpdated(payment, model.MemberId);

            _paymentRepository.Update(payment);
            await _paymentRepository.CommitAsync();
            await _bus.SendAsync(PrepareClaimTransaction(model.Id, ClaimTransactionType.updatePaymentSummary), Topics.RT_Billing_ProcessClaimTxn);
        }

        public async Task UpdatePaymentSummaryAsync(UpdatePaymentSummary model)
        {
            var payment = await _paymentRepository.GetByIdAsync(model.Id);

            payment.DepositDate = model.DepositDate;
            payment.EraPaymentMethod = GetEraPaymentMethod((PaymentMethods)model.PaymentMethodId);
            payment.PaymentMethodId = model.PaymentMethodId;
            payment.PostDate = model.PostDate;
            payment.PaymentAmount = model.PaymentAmount;

            _paymentRepository.Update(payment);
            await _paymentRepository.CommitAsync();
            await _bus.SendAsync(PrepareClaimTransaction(model.Id, ClaimTransactionType.updatePaymentSummary), Topics.RT_Billing_ProcessClaimTxn);
        }

        public async Task<PaymentShortInfo> GetPaymentShortInfoAsync(int paymentId)
        {
            var result = await _paymentRepository.Query()
                .Where(p => p.Id == paymentId)
                .Select(p => new PaymentShortInfo
                {
                    Id = p.Id,
                    ErrorsCount = p.PaymentClaims.Where(x => x.DateDeleted == null).Count(x => x.PaymentClaimErrors.Any()),
                    PaymentIdentifier = p.PaymentIdentifier,
                    ReconcileStatus = p.IsManualReconciled ? "Fully"
                        : (p.PaymentClaims.Where(x => x.DateDeleted == null).Sum(c => c.TotalPayment) ?? 0) == 0 ? "None"
                        : (p.PaymentClaims.Where(x => x.DateDeleted == null).Sum(c => c.TotalPayment) ?? 0) >= p.PaymentAmount ? "Fully"
                        : "Partially",
                    IsManual = p.PaymentTypeId != (int)PaymentTypes.ERAReceived,
                    IsPatientType = p.PaymentTypeId == (int)PaymentTypes.ClientPayment,
                    IsOtherType = p.PaymentTypeId == (int)PaymentTypes.OtherPayment,
                    IsInsuranceType = p.PaymentTypeId == (int)PaymentTypes.InsurancePayment || p.PaymentTypeId == (int)PaymentTypes.ERAReceived
                }).FirstOrDefaultAsync();

            return result;
        }

        public async Task<int> PostManualPaymentAsync(int paymentId)
        {
            var paymentEntity = await _paymentRepository.GetByIdAsync(paymentId);
            var paymentModel = new ManualCreatePaymentModel
            {
                AccountInfoId = paymentEntity.AccountInfoId ?? 0,
                PaymentMethod = ((PaymentMethods)paymentEntity.PaymentMethodId).ToString(),
                DepositDate = paymentEntity.DepositDate,
                PostDate = paymentEntity.PostDate,
                PaymentAmount = paymentEntity.PaymentAmount
            };

            if (paymentEntity.FunderID != null)
            {
                paymentModel.FunderType = "Insurance";
                paymentModel.FunderId = paymentEntity.HcFunderId;
            }

            var newPaymentId = await CreateManualPatientPaymentAsync(paymentModel);
            return newPaymentId;
        }

        public async Task<int> CreateManualPatientPaymentAsync(ManualCreatePaymentModel model)
        {
            var method = await _paymentMethodService.GetPaymentMethodByName(model.PaymentMethod);
            var paymentEntity = _mapper.Map<PaymentEntity>(model);
            paymentEntity.PaymentMethodId = method.Id;
            paymentEntity.DepositDate = model.DepositDate;
            paymentEntity.PostDate = model.PostDate;
            paymentEntity.Status = PaymentStatus.Unapplied;
            paymentEntity.IsManualPayment = (PaymentMethods)method.Id == PaymentMethods.RevSpring ? false : true;
            paymentEntity.IsManualReconciled = false;
            paymentEntity.PaymentAmount = model.PaymentAmount;
            paymentEntity.AccountInfoId = model.AccountInfoId;
            //paymentEntity.PaymentIdentifier = await GetNextPaymentID(model.AccountInfoId);
            paymentEntity.CreatedBy = model.MemberId;
            paymentEntity.HasAcknowledgedErrors = true;

            //TODO remove in prod!
            paymentEntity.IsTestData = true;
            paymentEntity.PaymentDate = EstDateTime;
            paymentEntity.ReceivedDate = EstDateTime;

            //i or h
            paymentEntity.TransactionHandlingCode = "H";


            if (model.FunderId.HasValue)
            {
                paymentEntity.HcFunderId = model.FunderId;
                var funder = await _rethinkServices.GetFunder(model.AccountInfoId, model.FunderId.Value);
                //var funder = await _funderRepository.GetByIdAsync(model.FunderId.Value);
                if (funder != null)
                    paymentEntity.FunderName = funder.funderName;
            }

            if (model.FunderType == "Insurance")
            {
                paymentEntity.PaymentTypeId = (int)PaymentTypes.InsurancePayment;
            }
            else if (model.FunderType == "Patient")
            {
                paymentEntity.PaymentTypeId = (int)PaymentTypes.ClientPayment;
            }
            else
            {
                paymentEntity.PaymentTypeId = (int)PaymentTypes.OtherPayment;
            }
            //Set Created Date and etc
            MarkCreated(paymentEntity, model.MemberId);

            paymentEntity.PaymentIdentifier = await GetNextPaymentID(model.AccountInfoId);

            var result = await _paymentRepository.AddAndGetAsync(paymentEntity);
            return result.Id;
        }


        public async Task<List<int>> DeletePaymentAsync(int[] paymentIds, int memberId, int accountInfoId)
        {
            var affectedClaimIds = new HashSet<int>();

            var payments = await _paymentRepository.Query()
                                .Where(p => paymentIds.Contains(p.Id)
                                 && p.AccountInfoId == accountInfoId
                                 && p.DateDeleted == null)
                                .Include(p => p.PaymentClaims.Where(pc => pc.DateDeleted == null))
                                    .ThenInclude(pc => pc.PaymentClaimServiceLines)
                                        .ThenInclude(sl => sl.PaymentClaimServiceLineAdjustments)
                                .AsSplitQuery()
                                .ToListAsync();

            if (!payments.Any())
                return new();

            foreach (var payment in payments)
            {
                foreach (var paymentClaim in payment.PaymentClaims)
                {
                    if (!paymentClaim.ClaimId.HasValue)
                        continue;

                    affectedClaimIds.Add(paymentClaim.ClaimId.Value);

                    foreach (var serviceLine in paymentClaim.PaymentClaimServiceLines)
                    {
                        foreach (var adjustment in serviceLine.PaymentClaimServiceLineAdjustments)
                        {
                            SoftDelete(adjustment, memberId);
                        }

                        SoftDelete(serviceLine, memberId);
                    }

                    SoftDelete(paymentClaim, memberId);
                }

                SoftDelete(payment, memberId);
            }
            await _paymentRepository.CommitAsync();

            return affectedClaimIds.ToList();
        }

        public async Task<List<int>> ReconcilePaymentAsync(int[] paymentIds, int memberId)
        {
            //var claimIds = new List<int>();
            var payments = new List<int>();
            List<ClaimTransactionModel> claimTransactionData = [];
            foreach (var paymentId in paymentIds)
            {
                try
                {
                    var payment = await (await _paymentRepository.GetAllAsync(x => x.Id == paymentId))
                    .Include(x => x.PaymentClaims).FirstOrDefaultAsync();

                    var paymentClaims = payment.PaymentClaims.Where(x => x.DateDeleted == null).ToList(); // get only active active payment claims

                    if (paymentClaims.Count == 0)
                    {
                        throw new Exception("Cannot reconcile payment without payment claims");
                    }

                    await ReconcileClaimsApiAsync(paymentClaims, payment, memberId, claimTransactionData);

                    payment.IsManualReconciled = true;
                    MarkUpdated(payment, memberId);
                    _paymentRepository.Update(payment);
                    payments.Add(payment.Id);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            await _paymentClaimRepository.CommitAsync();

            if (claimTransactionData.Count != 0)
            {
                //For Updating the statuses in AR report
                await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
            }
            return payments;
        }

        public async Task<int> ReconcileClaimAsync(int[] paymentId, int claimId, int memberId)
        {
            List<ClaimTransactionModel> claimTransactionData = [];
            try
            {
                var payment = await (await _paymentRepository.GetAllAsync(x => x.Id == paymentId[0]))
                .Include(x => x.PaymentClaims).FirstOrDefaultAsync();

                var paymentClaims = payment.PaymentClaims.Where(x => x.DateDeleted == null && x.ClaimId == claimId).ToList(); // get only active active payment claims

                if (paymentClaims.Count == 0)
                {
                    throw new Exception("Cannot reconcile payment without payment claims");
                }

                await ReconcileClaimsApiAsync(paymentClaims, payment, memberId, claimTransactionData);
            }
            catch (Exception)
            {
                throw new Exception("Error reconciling the claim");
            }
            await _paymentClaimRepository.CommitAsync();

            if (claimTransactionData.Count != 0)
            {
                await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
            }
            return paymentId[0];
        }

        private async Task<int> ReconcileClaimsApiAsync(IEnumerable<PaymentClaimEntity> claims, PaymentEntity payment, int memberId, List<ClaimTransactionModel> claimTransactionData)
        {
            var postedAmount = claims.Sum(c => c.TotalPayment) ?? 0;
            var balance = payment.PaymentAmount;

            if (postedAmount > balance || payment.PaymentTypeId == (int)PaymentTypes.ERAReceived)
                throw new Exception("Not valid payment");

            foreach (var paymentClaim in claims)
            {
                try
                {
                    if (paymentClaim.ClaimId.HasValue && payment.AccountInfoId.HasValue)
                    {
                        await _claimManagerService.UpdateClaimStatusAsync(paymentClaim.ClaimId.Value,
                            ClaimStatus.Closed,
                            payment.AccountInfoId.Value,
                            false);

                        await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                        {
                            ClaimId = paymentClaim.ClaimId ?? 0,
                            MemberId = memberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.ClaimReconciled,
                            ClaimHistoryAction = ClaimHistoryAction.ClaimReconciled,
                            NewValue = $"{payment.PaymentIdentifier}",
                        });

                        MarkUpdated(paymentClaim, memberId);
                        paymentClaim.ModifiedBy = payment.AccountInfoId.Value;
                        _paymentClaimRepository.Update(paymentClaim);
                        claimTransactionData.Add(PrepareClaimTransaction(paymentClaim.ClaimId.Value, ClaimTransactionType.submitClaim));
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return payment.Id;
        }

        public async Task<EOBPaymentInfo> GetEOBPaymentInfoAsync(int paymentId)
        {
            var result = await _paymentRepository.Query()
                .Where(p => p.Id == paymentId)
                .Select(p => new EOBPaymentInfo
                {
                    Id = p.Id,
                    PaymentAmount = p.PaymentAmount,
                    PaymentMethod = p.EraPaymentMethod,
                    CheckNumber = p.ReferenceNumber,
                    IssuedDate = p.DateCreated,
                    RecievedDate = p.ReceivedDate ?? EstDateTime,
                    AccountInfoId = p.AccountInfoId,

                    PayerRoutingNumber = p.FunderBankRouting,
                    PayerBankId = p.FunderBankAccount,
                    PayerId = p.FunderID,
                    PayerName = p.FunderName,
                    PayerPhoneNumber = p.FunderContactInfo,

                    PayeeId = p.PayeeId,
                    PayeeName = p.Payee,
                    PayeeBankId = p.PayeeBankRouting,
                    PayeeRoutingNumber = p.PayeeBankRouting,
                    PayeeAddress = (p.PayeeAddress1 ?? "") + " " + (p.PayeeAddress2 ?? "") + " " +
                                   (p.PayeeAddressCity ?? "") + " " + (p.PayeeAddressState ?? "") + " " +
                                   (p.PayeeAddressZip ?? "") + " " + (p.PayeeAddressCountry ?? ""),
                    PayeeAdressObject = new PayeeAddress
                    {
                        PayeeAddress1 = p.PayeeAddress1,
                        PayeeAddress2 = p.PayeeAddress2,
                        PayeeAddressCity = p.PayeeAddressCity,
                        PayeeAddressState = p.PayeeAddressState,
                        PayeeAddressZip = p.PayeeAddressZip,
                        PayeeAddressCountry = p.PayeeAddressCountry
                    }
                }).FirstOrDefaultAsync();

            // result.PayerName = "PayerName";
            // result.PayerLocation = "PayerLocation";
            // result.PayerPhoto = await GetLogo("");
            // result.PayerPhoneNumber = "PayerPhoneNumber";
            // result.PayerRoutingNumber = "PayerRoutingNumber";
            // result.PayerBankId = "PayerBankAccount";
            // result.PayerId = "PayerID";

            // result.PayeePhoto = await GetLogo("");
            // result.PayeeName = "PayeeName";
            // result.PayeeLocation = "PayeeLocation";
            // result.PayeeBankId = "PayeeBankId";
            // result.PayeeId = "PayeeAccountNumber";
            // result.PayeeAddress = "PayeeAddress";

            return result;
        }

        //public async Task<string> GetNextPaymentID(int accountInfoId)
        //{
        //    try
        //    {
        //        var maxID = await _paymentRepository.Query()
        //            .Where(p => p.AccountInfoId == accountInfoId)
        //            .MaxAsync(p => Convert.ToInt32(p.PaymentIdentifier));

        //        var nextPmtId = maxID + 1;
        //        return $"{nextPmtId:D8}";
        //    }
        //    catch (Exception)
        //    {
        //        return "1";
        //    }
        //}

        public async Task<string> GetNextPaymentID(int accountInfoId)
        {
            var maxId = await _paymentRepository.Query()
                .Where(p => p.AccountInfoId == accountInfoId)
                .Select(p => (int?)Convert.ToInt32(p.PaymentIdentifier))
                .MaxAsync() ?? 0;

            return (maxId + 1).ToString("D8");
        }

        public async Task<int> UploadFileAsync(EraUploadModelWithUserInfo model)
        {
            var guid = Guid.NewGuid().ToString();

            List<(MemoryStream, string)> list = new List<(MemoryStream, string)>();
            MemoryStream memoryStream = new MemoryStream(model.Data);
            list.Add((memoryStream, model.FileName));
            var uploadAvailityFilesModel = new UploadAvailityFilesModel { files = list, FilePath = _configuration["AvailityBackup"] };
            await _blobBackupService.UploadEDIResponseFilesToBlobBackup(uploadAvailityFilesModel);

            string eraData = System.Text.Encoding.UTF8.GetString(model.Data);
            var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(eraData);
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(transactionControlResult, model.AccountInfoId);

            var claimFile = $"{(result?.Id.ToString() ?? transactionControlResult.NpiNumber)}_{EstDateTime.ToString("yyMM")}manual.edi";

            var billingRequest = new BillingRequest
            {
                FieldIdentifier = claimFile,
                FolderName = BlobFolderNames.Incoming.ToString(),
                AccountInfoId = result?.Claim.AccountInfoId ?? model.AccountInfoId,
                Data = model.Data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = result?.Id ?? transactionControlResult.ControlNumbers.FirstOrDefault(),
                SubFolderName = null,
                ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), 1)
            };

            var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            string existingFileName = Path.GetFileName(billingFilePath);

            await _blobProcessingService.UploadIntoContainerAsync(_manualUploadBlobContainerName, guid, new MemoryStream(model.Data));
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            string newFileName = await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(model.Data));

            var paymentAttachmentEntity = new PaymentEraUploadEntity
            {
                CreatedBy = model.MemberId,
                FileName = newFileName,
                FilePath = billingFilePath.Replace(existingFileName, newFileName) ?? string.Empty,
                FileSize = model.Data.Length,
                FileMimeType = model.FileMimeType,
                BlobFileName = newFileName,
            };

            MarkCreated(paymentAttachmentEntity, model.MemberId);
            var eraUpload = await _paymentEraUploadRepository.AddAndGetAsync(paymentAttachmentEntity);

            return eraUpload.Id;
        }

        public async Task DeleteUploadAsync(IdWithUserInfo model)
        {
            var attachment = await _paymentEraUploadRepository.GetByIdAsync(model.Id);

            if (attachment.CreatedBy != model.MemberId)
            {
                throw new UnauthorizedAccessException("User does not own this era");
            }

            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(attachment.FilePath);
            await _blobProcessingService.DeleteBlobFromContainerAsync(containerName, fullFilePath);
            await _billingBlobService.DeleteBlobFromContainerAsync(containerName, fullFilePath);
            SoftDelete(attachment, model.MemberId);

            await _paymentEraUploadRepository.CommitAsync();
        }

        public async Task<List<PaymentProcessingModel>> GetProcessingPaymentsAsync(UserInfo userInfo)
        {
            var payments = await _paymentRepository
                .Query()
                .Where(x => !x.HasAcknowledgedErrors && x.AccountInfoId == userInfo.AccountInfoId)
                .Select(x => new PaymentProcessingModel
                {
                    PaymentId = x.Id,
                    PaymentStatus = x.Status,
                    FileName = x.PaymentEraUpload.FileName,
                    UploadId = x.PaymentEraUpload.Id
                })
                .ToListAsync();

            return payments;
        }

        public async Task StartPaymentParsingAsync(IdWithUserInfo model)
        {
            var file = await _paymentEraUploadRepository.GetByIdAsync(model.Id);
            if (file.CreatedBy != model.MemberId)
                return;

            await SendParseMessageAsync(file.FilePath, BillingConstants.BillingContainerName, model.AccountInfoId, file.Id);
        }


        private async Task SendParseMessageAsync(string filename, string containerName, int accountInfoId, int paymentEraUploadId)
        {
            var entityConnectionString = _serviceBusConnectionFactory.ConnectionStringBuilder.GetNamespaceConnectionString();
            entityConnectionString = $"{entityConnectionString};EntityPath={Queues.RT_Billing_EraDownloadProcess}";
            var messagePublisher = new MessagePublisher(entityConnectionString);
            var eraDownloadData = new EdiDownloadData
            {
                ContainerName = containerName,
                FileIdentifier = filename,
                DownloadDateTime = DateTimeExt.GetEasternDateTime(),
                AccountInfoId = accountInfoId,
                PaymentEraUploadId = paymentEraUploadId,
                ClearingHouseId = 1//Availity
            };

            await messagePublisher.SendAsync(eraDownloadData);
        }

        public async Task<PaymentAttachmentReturnModel> GetUploadAsync(IdWithUserInfo model)
        {
            var attachment = await _paymentEraUploadRepository.GetByIdAsync(model.Id);

            if (attachment.CreatedBy != model.MemberId)
            {
                throw new UnauthorizedAccessException("User does not own this attachment");
            }
            ;

            var splittedPath = attachment.FilePath.Split('/');
            var memoryStream =
                await _blobProcessingService.DownloadBlobFromContainerAsync(splittedPath[0], splittedPath[1]);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var attachmentReturnModel = new PaymentAttachmentReturnModel
            {
                MemoryStream = memoryStream,
                Filename = attachment.FileName
            };

            return attachmentReturnModel;
        }

        public async Task HideProcessingInfoAsync(HideProcessingInfoModelWithUserInfo model)
        {
            if (model.PaymentIds == null)
                return;

            var payments = await _paymentRepository.Query().Where(x => model.PaymentIds.Contains(x.Id)).ToListAsync();

            foreach (var payment in payments)
            {
                payment.HasAcknowledgedErrors = true;
                MarkUpdated(payment, model.MemberId);
            }

            await _paymentRepository.CommitAsync();
        }

        public async Task<string> GetFunderIdByPaymentIdAsync(int paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);

            return payment.FunderID;
        }

        private async Task<string> GetLogo(string logo)
        {
            if (string.IsNullOrWhiteSpace(logo))
            {
                return _defaultLogo;
            }

            try
            {
                var dowmloadUrl = ""; // await _fileManagerService.GetFileUrl(companyLogoUrl);
                return await GetImageAsBase64Url(dowmloadUrl);
            }
            catch
            {
                return _defaultLogo;
            }
        }

        private readonly string _defaultLogo =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAANsAAACkCAMAAAApKatmAAAAMFBMVEX////BwcG/v7/f39/v7+/7+/vPz8/z8/Pn5+fLy8vHx8fX19fj4+P39/fr6+vT09OFCFGdAAAC+UlEQVR4nO2a63KDIBCFVUDxEvP+b9u4BAXRpOksK+mc709lRw+cLPdpVQEAAAAAAAAAAAAAAAAAAIDvpH7BlZVnloe3TJVnloe3TJWzyXMI8QFvkkJ8wJukEB/wJinEB7xJCvEBb5JCfMCbpBAf8CYpxAe8SQrxAW+SQnzAm6QQHwU2CQAAAAAAALCiJ6Wm7jisk3BnlVLaSDTs9+jmgdpHjaobYlCH4VpFNuzgws14y93eTzj01j0tLPRbc7shCG8pNe0Wrmehdv+GI2+htUdzzeuw6cNwU5C5A2+GPAyT1q4Lts+wy9qorBqCcNW6Tqr1ROG6nG554O1OFujR5cTSsyLH1BXNuIUt9VCXRArfxdr+jtSbDpPSkaHlibJZ+1HWryaGMFeUuXRyvYjU2xiNmrsvTdGL81JaHuyW5KR0Nak3ys9aWltLw2qb+VtlaZUj7+t6Z9Y0l0DirQu7ZFXdaDg9HvzfHbVPoIPGZymzSeLN7gKNa3x3Mk3sLMdpvJjEm9oFetcXj/cv8cTjv57yNfcj3nprXSaC95RHn3hLf4Jr+IM3vwNZiv/Am/nIW7F9cn4xl7RbxL/1VXPJbg0w4RrgVi694D/brQFt0WvAbu2mNC5rd7xyGf/ZLlFN9PHFvNtzraV4z7Wugt+159LhENr2yrRB8Yc2d96hz+ogn6b0vbIbM+7Hd8dRd5ihDLrDjLmvc4mbGZ/hfjdrXoxuIpaGdf5sOo9N0NgbhevRWn9r4n4SMrScTV24tLNp7M2NmpXeb/6jcDt4b/FVQzELQHXsrbLhXdB2rrFhdFq7cnhFVBcz2KoTb9VtfJYHG77c+RstZZZVwA/Ts6u9UjH6sRm2SRZu8yM8pw5OrmQBAAAAAAAA0hT4j8/4P3NJIT7gTVKID3iTFOID3iSF+IA3SSE+4E1SiA94kxTiA94khfiAN0khPuBNUogPeJMU4oPX2wkc+n+tPLM8vGWqPLM8vGWqPHvdAAAAAAAAAAAAAAAAAAD4An4A5QcTnXnPO7wAAAAASUVORK5CYII=";

        private async Task<string> GetImageAsBase64Url(string url)
        {
            using (var client = new HttpClient())
            {
                var bytes = await client.GetByteArrayAsync(url);
                return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
            }
        }

        private string GetEraPaymentMethod(PaymentMethods paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethods.ACH:
                    return "ACH";
                case PaymentMethods.Check:
                    return "CHK";
                case PaymentMethods.NonPayment: // For a "NON" payment, the method does not really matter
                    return "NON";
                default:
                    return paymentMethod.ToString();
            }
        }

        public async Task<string> GetERAErrors(ERAUploadModel paymentIds)
        {
            var contentText = "";
            bool isErrorFound = false;
            var payments = await _paymentRepository.Query().Where(x => paymentIds.PaymentIds.Contains(x.Id)).Include(x => x.PaymentErrors).Include(x => x.PaymentEraUpload).ToListAsync();

            if (payments.Any())
            {
                foreach (var item in paymentIds.PaymentIds)
                {
                    var data = payments.Where(x => x.Id == item).FirstOrDefault();
                    if (data != null)
                    {
                        var fileName = data.PaymentEraUpload?.FileName;
                        string paymentErrorText = $"File name: {fileName}\n\n";

                        if (data.PaymentErrors.Any())
                        {
                            var i = 1;
                            isErrorFound = true;
                            paymentErrorText += $"Payment Errors:\n";
                            var errors = data.PaymentErrors.ToList();
                            foreach (var error in errors)
                            {
                                paymentErrorText += $"{i}. {error.ErrorMessage}\n";
                                i++;
                            }
                        }

                        var paymentClaims = await _paymentClaimRepository.Query().Where(x => x.PaymentId == item).Include(x => x.PaymentClaimErrors).ToListAsync();
                        if (paymentClaims.Any())
                        {
                            foreach (var claim in paymentClaims)
                            {
                                if (claim.PaymentClaimErrors.Any())
                                {
                                    int j = 1;
                                    isErrorFound = true;
                                    paymentErrorText += $"\nPayment Claim Errors:\n";
                                    paymentErrorText += $"Claim Identifier: {claim.ClaimIdentifier}\n";
                                    foreach (var claimError in claim.PaymentClaimErrors)
                                    {
                                        paymentErrorText += $"{j}. {claimError.ErrorMessage}\n";
                                        j++;
                                    }
                                }

                                var serviceLines = await _paymentClaimServiceLineRepository.Query().Where(x => x.PaymentClaimId == claim.Id).Include(x => x.PaymentClaimServiceLineErrors).ToListAsync();

                                if (serviceLines.Any())
                                {
                                    foreach (var serviceLine in serviceLines)
                                    {
                                        if (serviceLine.PaymentClaimServiceLineErrors.Any())
                                        {
                                            int k = 1;
                                            isErrorFound = true;
                                            paymentErrorText += $"\nPayment Claim Service Line Errors:\n";
                                            paymentErrorText += $"Service Code: {serviceLine.ServiceCode}:\n";
                                            foreach (var serviceLineError in serviceLine.PaymentClaimServiceLineErrors)
                                            {
                                                paymentErrorText += $"{k}. {serviceLineError.ErrorMessage}\n";
                                                k++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        paymentErrorText += "-------------------------------------------------------------------------------------\n";
                        contentText += paymentErrorText;
                    }
                }
            }
            return isErrorFound ? contentText : ""; // return content only if errors found
        }
        public List<ClaimTransactionModel> PrepareClaimTransactions(List<ClaimTransactionModel> claimTransactionData, List<int> serviceLineIdsToSend, int paymentTypeId)
        {
            ClaimTransactionType paymentType = (ClaimTransactionType)FindClaimTransactionTypeId((PaymentTypes)paymentTypeId);

            foreach (var serviceLineId in serviceLineIdsToSend)
            {
                claimTransactionData.Add(PrepareClaimTransaction(serviceLineId, ClaimTransactionType.deleteChargePayment));
            }
            return claimTransactionData;
        }

        public async Task AddUnAllocatedPayments(UnAllocatedPaymentsModel model)
        {
            var item = await _unAllocatedPaymentRepository.Query()
                        .Where(x => x.PaymentId == model.PaymentId
                                    && x.ChildProfileId == model.ChildProfileId
                                   //& x.AccountInfoId == model.AccountInfoId
                                    && x.DateDeleted == null)
                        .FirstOrDefaultAsync();

            if (item != null)
            {
                // Update existing record
                item.ChildProfileId = model.ChildProfileId;
                item.UnAllocatedAmount = model.UnAllocatedAmount == 0
                    ? item.UnAllocatedAmount
                    : model.UnAllocatedAmount;

                item.Notes = string.IsNullOrWhiteSpace(model.Notes)
                    ? item.Notes
                    : model.Notes;

                item.GuarantorContactId = model.GuarantorContactId;

                MarkCreated(item, model.MemberId);
                _unAllocatedPaymentRepository.Update(item);
            }
            else
            {
                // Create new record
                var newPayment = new UnAllocatedPaymentEntity
                {
                    AccountInfoId = model.AccountInfoId,
                    PaymentId = model.PaymentId,
                    ChildProfileId = model.ChildProfileId,
                    UnAllocatedAmount = model.UnAllocatedAmount == 0 ? 0 : model.UnAllocatedAmount,
                    Notes = model.Notes,
                    GuarantorContactId = model.GuarantorContactId
                };

                MarkCreated(newPayment, model.MemberId);
                _unAllocatedPaymentRepository.Add(newPayment);
            }

            await _unAllocatedPaymentRepository.CommitAsync();
        }

        public async Task<UnAllocatedPaymentsModel> GetUnAllocatedPaymentsById(UnAllocatedPaymentRequestModel model)
        {
            var query =  _unAllocatedPaymentRepository.Query().AsNoTracking()
                .Where(x => x.PaymentId == model.PaymentId
                            && x.ChildProfileId == model.ChildProfileId
                            && x.DateDeleted == null)
                              .OrderByDescending(x => x.DateCreated).FirstOrDefaultAsync();

            var result = new UnAllocatedPaymentsModel();

            if (query != null)
            { 
                result = new UnAllocatedPaymentsModel
                {
                    Id = query.Id,
                    PaymentId = model.PaymentId,
                    ChildProfileId = model.ChildProfileId,
                    UnAllocatedAmount = query.Result.UnAllocatedAmount,
                    Notes = query.Result.Notes,
                    AccountInfoId = model.AccountInfoId,
                    MemberId = model.MemberId
                };
            }

            return result ?? new UnAllocatedPaymentsModel();
        }

        public async Task<RethinkGuarantorDetails.ClientModel> GetGuarantorDetailsById(ClientHistoryUserInfo model)
        {
            var cacheKey = GetGuarantorDetailsKey(model.AccountInfoId);
            var guarantorDetails = await _cacheService.GetOrSetCacheAsync(
                cacheKey,
                async () => await _rethinkServices.GetClientDetailsGuarantor(model.AccountInfoId),
                TimeSpan.FromMinutes(cacheExpiration)
            );

            var guarantor = guarantorDetails.FirstOrDefault(x => x.UserId == model.ClientId);

            // Return the found guarantor, or null if not found
            return guarantor ?? new RethinkGuarantorDetails.ClientModel(); 
        }

        public string GetGuarantorDetailsKey(int accountId)
        {
            // Returning the cacheKey with accountId
            return string.Format("{0}{1}", guarantorDetailsCodeKey, accountId);
        }
        public string GetAccountChildCodeKey(int accountId)
        {
            // Returning the cacheKey with accountId
            return string.Format("{0}{1}", accountChildCodeKey, accountId);
        }
              
        public async Task<ChildProfileEntityModel> GetPatientAccountDetails(int accountId,int patientId)
        {
            var cacheKey = GetAccountChildCodeKey(accountId);
            var childProfiles= await _cacheService.GetOrSetCacheAsync(
                cacheKey,
                async () => await _rethinkServices.GetChildProfilesForAccount(accountId),
                TimeSpan.FromMinutes(cacheExpiration)
            );
            return childProfiles.FirstOrDefault(x=>x.Id==patientId);
        }
    }
}