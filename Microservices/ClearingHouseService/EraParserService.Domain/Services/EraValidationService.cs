using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services
{

    /*Saurabh :- We need to remove BhSimpleContext form the code */
    public class EraValidationService : BaseService, IEraValidationService
    {
        private class LookupResult<T>
        {
            public LookupResult(string error)
            {
                Error = error;
            }
            public LookupResult(T entity)
            {
                Entity = entity;
            }
            public LookupResult(List<T> entities)
            {
                Entities = entities;
            }


            public string Error { get; set; }
            public T Entity { get; set; }
            public List<T> Entities { get; set; }
        }


        private readonly ILoggerFactory _loggerFactory;
        private readonly BillingDbContext _billingDbContext;
        //private readonly BhSimpleContext _bhContext;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IMessageBus _bus;
        private readonly IPaymentService _paymentService;
        private readonly ILogger _logger;
        private bool _hasErrors = false;
        private string _fileID;        
       
        private readonly IRethinkMasterDataMicroServices _rethinkServices;

        public EraValidationService(
            ILoggerFactory loggerFactory,
            BillingDbContext billingDbContext,            
            IClaimHistoryService claimHistoryService, IPaymentService paymentService,
            IMessageBus bus, IRethinkMasterDataMicroServices rethinkServices)
        {
            _loggerFactory = loggerFactory;
            _billingDbContext = billingDbContext;            
            _claimHistoryService = claimHistoryService;
            _paymentService = paymentService;          
            _logger = _loggerFactory.CreateLogger(GetType());            
            _bus = bus;
            _rethinkServices = rethinkServices;
        }

        public async Task ValidateEraPayments(int accountInfoId, string fileID, List<PaymentEntity> eraPayments)
        {
            _fileID = fileID;

            foreach (var eraPayment in eraPayments)
            {
                try
                {
                    if (!eraPayment.IsErrorPayment)
                    {
                        await ValidateEraPayment(accountInfoId, eraPayment);
                    }
                }
                catch (Exception ex)
                {
                    await AddPaymentError(eraPayment, ex, $"{fileID}, Validating ERA for account # {accountInfoId} with InterchangeControlNumber '{eraPayment.InterchangeControlNumber}'");
                }
            }
        }

        private async Task ValidateEraPayment(int accountInfoId, PaymentEntity payment)
        {
            //var acctInfoIds = await ValidateAccountInfo(accountInfoId, payment);
            //if (acctInfoIds.Any())
            if(payment != null)
            {
                //int accountId = acctInfoIds.FirstOrDefault(x => x == accountInfoId);
                int accountId = accountInfoId;
                payment.AccountInfoId = accountId;                
                var funder = await ValidateFunder(accountId, payment);
                payment.HcFunderId = funder?.Id;
                foreach (var claim in payment.PaymentClaims)
                {
                    await ValidateClaim(accountId, funder?.Id, claim);
                }
            }
            await _billingDbContext.SaveChangesAsync();
            await AddClaimHistory(payment);
        }

        private async Task AddClaimHistory(PaymentEntity payment)
        {
            foreach (var pmtClaim in payment.PaymentClaims)
            {
                if (pmtClaim.Claim == null || !pmtClaim.ClaimId.HasValue)
                {
                    continue;
                }

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = pmtClaim.ClaimId.Value,
                    MemberId = payment.CreatedBy,
                    Mode = ClaimActionMode.System,
                    ClaimAction = ClaimAction.PaymentApplied,
                    ClaimHistoryAction = ClaimHistoryAction.PaymentAppliedElectronic,
                    NewValue = $"{payment.PaymentIdentifier}"
                });
            }
        }

        private async Task ValidateClaim(int acctInfoId,
                                         int? funderId,
                                         PaymentClaimEntity paymentClaim)
        {
            try
            {

                // hcClaimId / claimIdentifier
                var claimResult = await GetClaimByIdentifier(paymentClaim.ClaimIdentifier);
                if (claimResult.Entity == null)
                {
                    await AddClaimError(paymentClaim, $"Could not find claim # {paymentClaim.ClaimIdentifier}. {claimResult.Error}", PaymentErrorSeverity.Error, EraErrorType.Claim);
                }
                else
                {
                    paymentClaim.ClaimId = claimResult.Entity.Id;
                    paymentClaim.ChildProfileId = claimResult.Entity.ChildProfileId;

                    // childProfileId / clientIdentifier
                    var childProfileByIdentifierResult = await GetChildProfileByPolicy(acctInfoId, paymentClaim.ClientIdentifier);
                    var childProfileByIdentifierResultEntity = childProfileByIdentifierResult.Entity.First();
                    if (childProfileByIdentifierResultEntity == null)
                    {
                        await AddClaimError(paymentClaim, $"Could not find client information for insurance policy # {paymentClaim.ClientIdentifier}. {childProfileByIdentifierResult.Error}", PaymentErrorSeverity.Error, EraErrorType.ChildProfile);
                    }

                    // validate child profile against the child profile in the claim
                    if (childProfileByIdentifierResultEntity != null)
                    {
                        if (!childProfileByIdentifierResult.Entity.Where(x => x.userId == claimResult.Entity.ChildProfileId).Any())
                        {
                            await AddClaimError(paymentClaim, $"Client on claim {paymentClaim.ClaimIdentifier} does not match client specified by insurance policy # {paymentClaim.ClientIdentifier}.", PaymentErrorSeverity.Error, EraErrorType.ChildProfile);
                        }

                    }

                    if (paymentClaim.ClaimStatus == "4")
                    {
                        UpdateClaimStatus(claimResult.Entity, ClaimStatus.RejectedFunder);
                        await _bus.SendAsync(PrepareClaimTransaction(claimResult.Entity.Id, ClaimTransactionType.submitClaim), Topics.RT_Billing_ProcessClaimTxn);
                    }

                    foreach (var pmtClaimAdj in paymentClaim.PaymentClaimAdjustments)
                    {
                        await ValidateClaimAdjustment(claimResult.Entity.Id, paymentClaim, pmtClaimAdj);
                    }

                    foreach (var serviceLine in paymentClaim.PaymentClaimServiceLines)
                    {
                        await ValidateClaimServiceLine(acctInfoId, funderId, paymentClaim, claimResult.Entity, serviceLine);
                    }
                }
            }
            catch (Exception ex)
            {
                await AddClaimError(paymentClaim, ex, $"Validating Claim with ClaimIdentifier '{paymentClaim.ClaimIdentifier}', {paymentClaim.ClientFirstName} {paymentClaim.ClientFirstName} ({paymentClaim.ClaimIdentifier})");
            }
        }

        private async Task<LookupResult<List<ClientUserContact>>> GetChildProfileByPolicy(int accountInfoId, string clientIdentifier)
        {
            if (string.IsNullOrWhiteSpace(clientIdentifier))
            {
                return new LookupResult<List<ClientUserContact>>($"ClientIdentifier '{clientIdentifier}' missing in ERA");
            }
            if (int.TryParse(clientIdentifier, out var clientId))
            {
                //var contacts = new List<ChildProfileEntityModel>();

                //var contacts = await GetInsuranceContactByPolicyNo(clientIdentifier);
                var contacts = await _rethinkServices.GetInsuranceContactByPolicy(clientIdentifier);
                var clientIds = contacts.Select(c => c.userId);

                // now find the child profile record for this account that has the specified InsurancePolicyNumber (ClientIdentifier)
                var claims = new List<ChildProfileEntityModel>();
                return ValidateListResult(contacts, $"ClientIdentifier '{clientIdentifier}'");
            }

            return new LookupResult<List<ClientUserContact>>($"ClientIdentifier '{clientIdentifier}' in ERA is not a valid number");
        }

        private async Task<LookupResult<ClaimEntity>> GetClaimByIdentifier(string claimIdentifier)
        {
            if (string.IsNullOrWhiteSpace(claimIdentifier))
            {
                return new LookupResult<ClaimEntity>($"ClaimIdentifier '{claimIdentifier}' missing in ERA");
            }

            var claims = await _billingDbContext.Claims.Where(clm => clm.ClaimIdentifier == claimIdentifier)
                                                             .ToListAsync();
            return ValidateResult(claims, $"ClaimIdentifier '{claimIdentifier}'");


            //return new LookupResult<ClaimEntity>($"ClaimIdentifier '{claimIdentifier}' in ERA is not a valid number");
        }

        private async Task ValidateClaimServiceLine(int acctInfoId,
                                                    int? funderId,
                                                    PaymentClaimEntity paymentClaim,
                                                    ClaimEntity rtClaim,
                                                    PaymentClaimServiceLineEntity serviceLine)
        {
            var slBillInfo = $"{serviceLine.DateOfService ?? serviceLine.ServiceStartDate}: " +
                             $"{serviceLine.ServiceCode} " +
                             $"{serviceLine.ChargeAmount},";

            try
            {
                // hcChargeEntryId / (dateOfService | serviceStartDate), serviceCode, procedureModifier1, procedureModifier2, procedureModifier3, procedureModifier4
                var chargeEntryResult = await GetChargeEntry(paymentClaim,
                                                              rtClaim,
                                                              serviceLine.DateOfService ?? serviceLine.ServiceStartDate,
                                                              serviceLine.ServiceCode,
                                                              serviceLine.ChargeAmount);
                if (chargeEntryResult?.Entity != null)
                {
                    serviceLine.ClaimChargeEntryId = chargeEntryResult.Entity.Id;
                    serviceLine.ExpectedAmount = serviceLine.ChargeAmount;
                }
                else
                {
                    var error = (chargeEntryResult == null) ? $"'{slBillInfo}' not found" : chargeEntryResult.Error;
                    await AddClaimServiceLineError(serviceLine, $"Could not find Claim Charge Entry. {error}", PaymentErrorSeverity.Error, EraErrorType.ChargeEntry);
                }


                if (!serviceLine.AllowedAmount.HasValue)
                {
                    serviceLine.AllowedAmount = CalculateAllowedAmount(serviceLine);
                }
                foreach (var serviceLineAdj in serviceLine.PaymentClaimServiceLineAdjustments)
                {
                    await ValidateClaimServiceLineAdjustment(rtClaim.Id, paymentClaim.ClaimStatus, serviceLineAdj, serviceLine);
                }
            }
            catch (Exception ex)
            {
                await AddClaimServiceLineError(serviceLine, ex, $"Validating Claim Service Line '{slBillInfo}'");
            }
        }
        private async Task ValidateClaimAdjustment(int claimId, PaymentClaimEntity paymentClaim, PaymentClaimAdjustmentEntity pmtClaimAdj)
        {
            async Task AddError(Exception ex)
            {
                var msg = $"Unknown reason ({pmtClaimAdj.AdjustmentGroupCode ?? "<no-group>"}:{pmtClaimAdj.AdjustmentReasonCode ?? "<no-reason>"}) " +
                          $"in claim with ClaimIdentifier '{paymentClaim.ClaimIdentifier}'";
                if (ex == null)
                {
                    await AddClaimError(paymentClaim, msg, PaymentErrorSeverity.Warning, EraErrorType.AdjustmentReason);

                }
                else
                {
                    await AddClaimError(paymentClaim, ex, msg);
                }
            }
            try
            {
                // lookup reason code
                var reasonCode = _billingDbContext.PaymentAdjustmentReasons.FirstOrDefault(rc => rc.GroupCode == pmtClaimAdj.AdjustmentGroupCode &&
                                                                                                rc.AdjustmentCode == pmtClaimAdj.AdjustmentReasonCode);
                if (reasonCode == null)
                {
                    await AddError(null);
                }
                else
                {
                    pmtClaimAdj.PaymentAdjustmentReasonId = reasonCode.Id;
                    // denied
                    if (paymentClaim.ClaimStatus == "4")
                    {
                        await ProcessEraDeniedStatus(claimId, reasonCode, null, AdjustmentLevel.Claim);
                    }
                }

            }
            catch (Exception ex)
            {
                await AddError(ex);
            }
        }

        private async Task ValidateClaimServiceLineAdjustment(int claimId, string claimStatus, PaymentClaimServiceLineAdjustmentEntity serviceLineAdj,
                                                        PaymentClaimServiceLineEntity serviceLine)
        {
            async Task AddError(Exception ex)
            {
                var msg =
                    $"Unknown reason ({serviceLineAdj.AdjustmentGroupCode ?? "<no-group>"}:{serviceLineAdj.AdjustmentReasonCode ?? "<no-reason>"}) " +
                    $"in service line with ClaimIdentifier '{serviceLine.PaymentClaim.ClaimIdentifier}' and " +
                    $"service line {serviceLine.DateOfService}:{serviceLine.ServiceCode} " +
                    $"{serviceLine.ProcedureModifier1} {serviceLine.ProcedureModifier2} " +
                    $"{serviceLine.ProcedureModifier3} {serviceLine.ProcedureModifier4}";
                if (ex == null)
                {
                    await AddClaimServiceLineError(serviceLine, msg, PaymentErrorSeverity.Warning, EraErrorType.AdjustmentReason);

                }
                else
                {
                    await AddClaimServiceLineError(serviceLine, ex, msg);
                }
            }
            try
            {
                // lookup reason code
                var reasonCode = _billingDbContext.PaymentAdjustmentReasons.FirstOrDefault(rc => rc.GroupCode == serviceLineAdj.AdjustmentGroupCode &&
                                                                                                rc.AdjustmentCode == serviceLineAdj.AdjustmentReasonCode);
                if (reasonCode == null)
                {
                    await AddError(null);
                }
                else
                {
                    serviceLineAdj.PaymentAdjustmentReasonId = reasonCode.Id;
                    // denied
                    if (claimStatus == "4")
                    {
                        await ProcessEraDeniedStatus(claimId, reasonCode, serviceLine.RemittanceRemarkCode2, AdjustmentLevel.ServiceLine);
                    }
                }

            }
            catch (Exception ex)
            {
                await AddError(ex);
            }
        }

        private decimal? CalculateAllowedAmount(PaymentClaimServiceLineEntity serviceLine)
        {
            /*
             A: Get Billed amount from SVC-02 (ChargeAmount)
             B: Get charges that exceed contracted data/Fee schedule from CAS03 
                when CAS 01=CO and CAS02=42 
                (CO= Contractual obligation and 
                 42= Charges exceed fee schedule or maximum allowable amount)
             Allowed Amount= A-B

             Finally, if A or B cannot be derived as value not present for a service line, set the Allowed Amount as Null/Blank
             */
            if (serviceLine.ChargeAmount.HasValue)
            {
                return serviceLine.ChargeAmount.Value;
                //var adjustments = serviceLine.PaymentClaimServiceLineAdjustments.Where(sla =>
                //        sla.AdjustmentGroupCode == "CO" &&
                //        sla.AdjustmentReasonCode == "42")
                //    .ToList();
                //if (adjustments.Any())
                //{
                //    var adjAmt = adjustments.Sum(sla => sla.AdjustmentAmount);
                //    if (adjAmt.HasValue)
                //    {
                //        return serviceLine.ChargeAmount - Math.Abs(adjAmt.Value);
                //    }
                //}
            }

            return null;



        }


        private async Task<LookupResult<ClaimChargeEntryEntity>> GetChargeEntry(PaymentClaimEntity paymentClaim,
                                                               ClaimEntity rtClaim,
                                                               DateTime? dos,
                                                               string serviceCode,
                                                               decimal? chargeAmount)
        {
            if (!dos.HasValue)
            {
                return new LookupResult<ClaimChargeEntryEntity>($"No Date Of Service specified for claim {paymentClaim.ClaimIdentifier}");
            }

            var slDesc = $"Service Line {dos.Value.ToShortDateString()} {serviceCode} {chargeAmount}";
            if (rtClaim == null)
            {
                return new LookupResult<ClaimChargeEntryEntity>($"Cannot look up charge entry for {slDesc}. Claim not found for claim identifier {paymentClaim.ClaimIdentifier}");
            }

            var chargeEntries = await _billingDbContext.ClaimChargeEntries.Where(ce => ce.ClaimId == rtClaim.Id &&
                                                                            ce.BillingCode == serviceCode &&
                                                                            ce.DateOfService == dos &&
                                                                            ce.Charges == chargeAmount)
                                                               .ToListAsync();

            return ValidateResult(chargeEntries, slDesc);
        }

        private async Task<FunderModel> ValidateFunder(int accountInfoId, PaymentEntity payment)
        {
            string funderPayerId = payment.FunderTaxID;
            string funderId = payment.FunderID;
            string funderName = payment.FunderName;

            var byId = await GetFunderById(accountInfoId, funderId);
            List<FunderModel> funders = byId?.Entity ?? new List<FunderModel>();

            string error = byId?.Error ?? "";
            if (!funders.Any())
            {
                var byTaxId = await GetFunderByTaxId(funderPayerId);
                funders = byTaxId?.Entity ?? new List<FunderModel>();
                error += $"; {byTaxId?.Error ?? ""}";
            }
            if (!funders.Any())
            {
                var byName = await GetFunderByName(funderName);
                funders = byName?.Entity ?? new List<FunderModel>();
                error += $"; {byName?.Error ?? ""}";
            }
            if (!funders.Any())
            {
                await AddPaymentError(payment,
                                      $"Could not find Funder by Id, TaxID or Name. Error = {error}",
                                      PaymentErrorSeverity.Warning,
                                      EraErrorType.Funder);
                return null;
            }
            return funders.FirstOrDefault(x => x.accountId == accountInfoId);
        }
        
        private LookupResult<T> ValidateResult<T>(IList<T> list, string contextMsg)
        {
            var count = list.Count();
            if (count > 1)
            {
                return new LookupResult<T>($"{count} matches found for {contextMsg}");
            }
            else if (count < 1)
            {
                return new LookupResult<T>($"No matches found for {contextMsg}");
            }
            else
            {
                return new LookupResult<T>(list.First());
            }
        }

        private LookupResult<List<T>> ValidateListResult<T>(List<T> list, string contextMsg)
        {
            var count = list.Count();
            if (count < 1)
            {
                return new LookupResult<List<T>>($"No matches found for {contextMsg}");
            }
            else
            {
                return new LookupResult<List<T>>(list);
            }
        }

        #region Funder helpers       

        private async Task<LookupResult<List<FunderModel>>> GetFunderById(int accountInfoId, string funderID)
        {
            if (string.IsNullOrWhiteSpace(funderID))
                return null;
            var funderData = await _rethinkServices.GetFunder(accountInfoId, Convert.ToInt32(funderID));            
            var funders = funderData != null
                ? new List<FunderModel> { MapToFunderModel(funderData) }
                : new List<FunderModel>();
            return ValidateListResult(funders, $"FunderID '{funderID}'");
        }

        private FunderModel MapToFunderModel(FunderDataModel funderData)
        {
            return new FunderModel
            {
                Id = funderData.id,
                accountId = funderData.accountId,
                FunderName = funderData.funderName,
                VendorId = funderData.vendorId
            };
        }

        private async Task<LookupResult<List<FunderModel>>> GetFunderByTaxId(string funderPayerId)
        {
            if (string.IsNullOrWhiteSpace(funderPayerId))
                return null;            
            var funderListModel = await _rethinkServices.GetFunderListByTaxId(funderPayerId);
            var funders = funderListModel?.data ?? new List<FunderModel>();
            return ValidateListResult(funders, $"funderPayerId '{funderPayerId}'");
        }
        private async Task<LookupResult<List<FunderModel>>> GetFunderByName(string funderName)
        {
            if (string.IsNullOrWhiteSpace(funderName))
                return null;            
            var funderListModel = await _rethinkServices.GetFunderListByName(funderName);
            var funders = funderListModel?.data ?? new List<FunderModel>();

            return ValidateListResult(funders, $"Name '{funderName}'");
        }

        #endregion

        #region Claim helpers
        private void UpdateClaimStatus(ClaimEntity claim, ClaimStatus status)
        {
            claim.ClaimStatus = status;
            MarkUpdated(claim, 0);

            _billingDbContext.Claims.Update(claim);
        }
        #endregion

        #region errors

        private async Task AddPaymentError(PaymentEntity payment, Exception exception, string msg)
        {
            await AddPaymentError(payment,
                                  $"Unexpected Exception: {msg}. Error = {exception.Message}",
                                  PaymentErrorSeverity.Fatal,
                                  EraErrorType.Exception);
        }

        private async Task AddPaymentError(PaymentEntity payment,
                                           string message,
                                           PaymentErrorSeverity severity,
                                           EraErrorType errorType)
        {
            var error = _paymentService.CreatePaymentError(payment, $"{_fileID}, {message}", severity, errorType);
            await _billingDbContext.PaymentErrors.AddAsync(error);
            _hasErrors = true;
        }

        private async Task AddClaimError(PaymentClaimEntity paymentClaim, Exception exception, string msg)
        {
            await AddClaimError(paymentClaim,
                                $"Unexpected Exception: {msg}. Error = {exception.Message}",
                                PaymentErrorSeverity.Fatal,
                                EraErrorType.Exception);
        }

        private async Task AddClaimError(PaymentClaimEntity paymentClaim,
                                          string message,
                                          PaymentErrorSeverity severity,
                                          EraErrorType errorType)
        {
            var error = _paymentService.CreatePaymentClaimError(paymentClaim, $"{_fileID}, {message}", severity, errorType);
            await _billingDbContext.PaymentClaimErrors.AddAsync(error);
            _hasErrors = true;
        }

        private async Task AddClaimServiceLineError(PaymentClaimServiceLineEntity serviceLine,
                                                    Exception exception, string msg)
        {
            await AddClaimServiceLineError(serviceLine,
                                           $"Unexpected Exception: {msg}. Error = {exception.Message}",
                                           PaymentErrorSeverity.Fatal,
                                           EraErrorType.Exception);
        }

        private async Task AddClaimServiceLineError(PaymentClaimServiceLineEntity serviceLine,
                                                    string message,
                                                    PaymentErrorSeverity severity,
                                                    EraErrorType errorType)
        {
            var error = _paymentService.CreateClaimServiceLineError(0, serviceLine, $"{_fileID}, {message}", severity, errorType);
            await _billingDbContext.PaymentClaimServiceLineErrors.AddAsync(error);
            _hasErrors = true;
        }

        private async Task ProcessEraDeniedStatus(int claimId, PaymentAdjustmentReasonEntity reasonCode, string remarkCode, AdjustmentLevel adjustmentLevel)
        {
            var predefinedErrorMessage = await _billingDbContext.ClaimErrorMessages.FirstOrDefaultAsync(x => x.ErrorNumber == ClaimErrorNumber.EraFunderDenied);
            var groupCode = await _billingDbContext.ExternalCodes.FirstOrDefaultAsync(x => x.Code == reasonCode.GroupCode);
            var adjustmentCode = await _billingDbContext.ExternalCodes.FirstOrDefaultAsync(x => x.Code == reasonCode.AdjustmentCode);
            var validationDate = EstDateTime;

            await AddEraValidationError(new ClaimValidationErrorEntity
            {
                ClaimId = claimId,
                ClaimErrorMessageId = predefinedErrorMessage.Id,
                ValidationDate = validationDate,
                ClaimErrorSource = ClaimErrorSource.Era,
                EraValidationError = new EraValidationErrorEntity
                {
                    AdjustmentLevel = adjustmentLevel,
                    GroupCodeId = groupCode.Id,
                    AdjustmentCodeId = adjustmentCode.Id,
                },
            });

            if (!string.IsNullOrEmpty(remarkCode))
            {
                // Remittance Advice Remark Code
                var rarc = await _billingDbContext.ExternalCodes.FirstOrDefaultAsync(x => x.Code == remarkCode);

                await AddEraValidationError(new ClaimValidationErrorEntity
                {
                    ClaimId = claimId,
                    ClaimErrorMessageId = predefinedErrorMessage.Id,
                    ValidationDate = validationDate,
                    ClaimErrorSource = ClaimErrorSource.Era,
                    EraValidationError = new EraValidationErrorEntity
                    {
                        AdjustmentLevel = adjustmentLevel,
                        GroupCodeId = rarc.Id,
                    },
                });
            }
        }

        private async Task AddEraValidationError(ClaimValidationErrorEntity errorEntity)
        {
            MarkCreated(errorEntity, 0);
            await _billingDbContext.ClaimValidationErrors.AddAsync(errorEntity);
        }

        #endregion
        
    }
}
