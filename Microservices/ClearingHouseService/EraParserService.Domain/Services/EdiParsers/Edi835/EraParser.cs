using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using EdiFabric.Core.Model.Edi;
using EdiFabric.Core.Model.Edi.ErrorContexts;
using EdiFabric.Core.Model.Edi.X12;
using EdiFabric.Templates.Common;
using EdiFabric.Templates.Hipaa5010;
using EraParserService.Domain.Enums;
using EraParserService.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Extensions;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services.EdiParsers.Edi835
{
    public class EraParserError
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }

    public class EraParser : BaseService
    {
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly BillingDbContext _billingDbContext;
        private readonly int _accountInfoId;
        private readonly int? _paymentEraUploadId;
        private readonly string _fileID;
        private readonly List<IEdiItem> _ediItems;
        private readonly string _ediText;

        private readonly string _isaId;
        private readonly string _transactionId;
        private readonly string _loop1000AId;
        private readonly string _loop1000BId;
        private readonly string _loop2100Id;
        private readonly string _loop2110Id;
        private readonly IEdiProcessingService _eraProcessingService;

        public EraParserError LastError { get; private set; }

        public EraParser(ILogger logger,
                         IPaymentService paymentService,
                         BillingDbContext billingDbContext,
                         int accountInfoId,
                         int? paymentEraUploadId,
                         string fileID,
                         List<IEdiItem> ediItems,
                         string ediText,
                         IEdiProcessingService eraProcessingService)
        {
            _logger = logger;
            _paymentService = paymentService;
            _billingDbContext = billingDbContext;
            _accountInfoId = accountInfoId;
            _paymentEraUploadId = paymentEraUploadId;
            _fileID = fileID;
            _ediItems = ediItems;
            _ediText = ediText;

            _loop2110Id = $"{_fileID}: Transaction/2100/2110";
            _isaId = $"{_fileID}: ISA";
            _transactionId = $"{_fileID}: Transaction";
            _loop1000AId = $"{_fileID}: Transaction/1000A";
            _loop1000BId = $"{_fileID}: Transaction/1000B";
            _loop2100Id = $"{_fileID}: Transaction/2100";
            _eraProcessingService = eraProcessingService;
        }

        public async Task<List<PaymentEntity>> ParseDocument(BillingFolderStructureModel billingBlobModel, string cId)
        {
            LogMsg($"Parsing ERA file: {_fileID} for Account {_accountInfoId}");
            var result = new List<PaymentEntity>();
            if (_accountInfoId <= 0)
            {
                var msg = $"{_fileID}, Error processing ERA. Could not find Account for id = {_accountInfoId}";
                var errorPmt = await CreateErrorPayment(_accountInfoId,
                                                         msg,
                                                         PaymentErrorSeverity.Fatal,
                                                         EraErrorType.Parsing,
                                                         PaymentStatus.ParsingError);
                LogError(msg);
                result.Add(errorPmt.Payment);

                return result;
            }

            try
            {
                var payments = await ProcessTransactions(_ediItems);
                payments.ForEach(doc => doc.EraDocumentEdi = _ediText);
                result.AddRange(payments);

            }
            catch (Exception ex)
            {
                await _eraProcessingService.uploadToBilling(billingBlobModel, BlobFolderNames.Failed, null);
                var msg = $"{_fileID}: Error parsing document. Error={ex.Message}";
                billingBlobModel.Message = ($"Error parsing 835 document - for ClaimIdentifier = '{cId}' Error = {ex.Message}\nTimestamp: {DateTime.UtcNow:G}");

                var errorPmt = await CreateErrorPayment(_accountInfoId,
                                                        msg,
                                                        PaymentErrorSeverity.Error,
                                                        EraErrorType.Parsing,
                                                        PaymentStatus.ParsingError);
                LogError(msg, ex);
                result.Add(errorPmt.Payment);
            }

            return result;
        }

        //private async Task<PaymentErrorEntity> CreateErrorPayment(int accountInfoId,
        //                                                          string msg,
        //                                                          PaymentErrorSeverity severity,
        //                                                          EraErrorType errorType,
        //                                                          PaymentStatus paymentStatus)
        //{
        //    var payment = await _paymentService.CreatePayment(accountInfoId, null, PaymentTypes.ERAReceived);
        //    payment.IsErrorPayment = true;
        //    var error = _paymentService.CreatePaymentError(payment, msg, severity, errorType, paymentStatus);
        //    await _billingDbContext.Payments.AddAsync(payment);
        //    await _billingDbContext.hcPaymentErrors.AddAsync(error);
        //    await _billingDbContext.SaveChangesAsync();

        //    return error;
        //}

        private async Task<PaymentErrorEntity> CreateErrorPayment(int accountInfoId,
                                                          string msg,
                                                          PaymentErrorSeverity severity,
                                                          EraErrorType errorType,
                                                          PaymentStatus paymentStatus)
        {
            var payment = await _paymentService.CreatePayment(accountInfoId, 0, _paymentEraUploadId, PaymentTypes.ERAReceived);
            payment.IsErrorPayment = true;
            var error = _paymentService.CreatePaymentError(payment, msg, severity, errorType, paymentStatus);
            // Assuming _billingDbContext is your DbContext instance
            payment.PaymentIdentifier = await GenerateNextIdentifier(accountInfoId);
            _billingDbContext.Payments.Add(payment);
            _billingDbContext.PaymentErrors.Add(error);
            await _billingDbContext.SaveChangesAsync();
            return error;
        }


        private async Task<List<PaymentEntity>> ProcessTransactions(List<IEdiItem> ediItems)
        {
            LogDebug("================================");

            var transactions = ediItems.OfType<TS835>().ToList();
            var isaItems = ediItems.OfType<ISA>().ToList();
            var geItems = ediItems.OfType<GE>().ToList();
            var gsItems = ediItems.OfType<GS>().ToList();

            var result = new List<PaymentEntity>();
            var curTransactionNum = 1;
            int expectedTransCount = 0;

            var isa = isaItems.FirstOrDefault();
            if (isa == null)
                throw new EraEdiException("Missing ISA Segment");

            for (int i = 0; i < transactions.Count; i++)
            {
                GE ge = null;
                if (i < geItems.Count)
                {
                    ge = geItems[i];
                }
                var transaction = transactions[i];

                ValidateTransaction(transaction);

                // TODO: after migrating to EdiFabric we could omit the serialization value that we pass to CreatePayment
                var xmlTransaction = transaction.Serialize();
                var payment = await _paymentService.CreatePayment(_accountInfoId, 0, _paymentEraUploadId, PaymentTypes.ERAReceived, xmlTransaction.ToString());     //member id 0 as a part of functionality here to show the record is created/modified by system.

                try
                {
                    payment.PaymentIdentifier=await GenerateNextIdentifier(_accountInfoId);
                    await _billingDbContext.Payments.AddAsync(payment);
                    await _billingDbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    LogError("Failed to save payment", ex);
                }
                try
                {
                    payment.TransactionCount = transactions.Count;
#if DEBUG
                    LogDebug(transaction.Serialize().ToString());
#endif


                    if (transaction == null)
                        throw new EraEdiException($"{_fileID}/{payment.PaymentIdentifier}: Missing Transaction {curTransactionNum}");

                    // if we have a GE node, check the transaction count matches
                    if (ge != null)
                    {
                        var geId = $"{_fileID}: GE transacation number: {curTransactionNum}";
                        expectedTransCount += GetRequiredInt(geId, ge.NumberOfIncludedSets_1, "NumberOfIncludedSets_1");
                    }


                    await ParseTransaction(payment, curTransactionNum, isa, transaction);
                    result.Add(payment);
                    curTransactionNum += 1;

                }
                catch (Exception ex)
                {
                    var msg = $"{_fileID}/{payment.PaymentIdentifier}: Error parsing transaction # {curTransactionNum}. Error = {ex.Message}";
                    var error = _paymentService.CreatePaymentError(payment,
                                                                   msg,
                                                                   PaymentErrorSeverity.Error,
                                                                   EraErrorType.Parsing,
                                                                   PaymentStatus.ParsingError);
                    await _billingDbContext.PaymentErrors.AddAsync(error);

                    LogError(msg, ex);
                }
            }

            if (expectedTransCount > 0 && expectedTransCount != transactions.Count)
            {
                throw new EraEdiException($"{_fileID}: Transaction count mismatch: expected={expectedTransCount}, processed={transactions.Count}");
            }

            LogMsg("================================");
            return result;
        }

        private async Task<string> GenerateNextIdentifier(int acctInfoId)
        {
            var maxPmtId = await _billingDbContext.Payments
                                                     .Where(p => p.AccountInfoId == acctInfoId)
                                                     .Select(p => (int?)Convert.ToInt32(p.PaymentIdentifier))
                                                     .MaxAsync() ?? 0;
            return (maxPmtId + 1).ToString("D8");
        }

        private void ValidateTransaction(TS835 transaction)
        {
            MessageErrorContext errorContext;
            if (!transaction.IsValid(out errorContext, new ValidationSettings { SyntaxSet = new Extended() }))
            {
                var errors = errorContext.Flatten();
                throw new EraEdiException($"{_fileID}: {string.Join(",", errors)}");
            }

        }

        private async Task ParseTransaction(PaymentEntity payment, int curTransactionNum, ISA isa, TS835 transaction)
        {
            try
            {
                var loop1000A = transaction.AllN1.Loop1000A;
                var loop1000B = transaction.AllN1.Loop1000B;
                var bpr = transaction.BPR_FinancialInformation;
                var loop1000Bn4 = loop1000B.N4_PayeeCity_State_ZIPCode;
                var loop1000Aper = loop1000A.AllPER.PER_PayerTechnicalContactInformation.FirstOrDefault();

                LogMsg($"Creating payment for transaction # {curTransactionNum}");
                // populate payment
                payment.ReceivedDate = EstDateTime;
                payment.PostDate = EstDateTime;
                payment.DepositDate = GetDate(bpr.CheckIssueorEFTEffectiveDate_16, "CheckIssueorEFTEffectiveDate_16");
                payment.InterchangeControlNumber = GetRequiredString(_isaId, isa.InterchangeControlNumber_13, "InterchangeControlNumber_13");
                payment.RequestsAck = GetBoolValue(isa.AcknowledgementRequested_14);
                payment.IsTestData = !"P".Equals(isa.UsageIndicator_15); // "P" = Production
                payment.TransactionHandlingCode = bpr.TransactionHandlingCode_01;
                payment.PaymentAmount = GetRequiredDecimal(_transactionId, bpr.TotalPremiumPaymentAmount_02, "TotalPremiumPaymentAmount_02");
                payment.CreditOrDebit = GetRequiredString(_transactionId, bpr.CreditorDebitFlagCode_03, "CreditorDebitFlagCode_03");
                payment.EraPaymentMethod = GetRequiredString(_transactionId, bpr.PaymentMethodCode_04, "PaymentMethodCode_04");

                payment.FunderBankRoutingQualifier = bpr.DepositoryFinancialInstitutionDFIIdentificationNumberQualifier_06;
                payment.FunderBankRouting = bpr.OriginatingDepositoryFinancialInstitutionDFIIdentifier_07;
                payment.FunderBankAccount = bpr.SenderBankAccountNumber_09;

                payment.PayeeBankRoutingQualifier = bpr.DepositoryFinancialInstitutionDFIIdentificationNumberQualifier_12;
                payment.PayeeBankRouting = bpr.ReceivingDepositoryFinancialInstitutionDFIIdentifier_13;
                payment.PayeeBankAccountQualifier = bpr.AccountNumberQualifier_14;
                payment.PayeeBankAccount = bpr.ReceiverBankAccountNumber_15;

                payment.PaymentTypeId = (int)PaymentTypes.ERAReceived;
                payment.PaymentDate = GetDate(bpr.CheckIssueorEFTEffectiveDate_16, "CheckIssueorEFTEffectiveDate_16");
                payment.ReferenceNumber = transaction.TRN_ReassociationTraceNumber.CurrentTransactionTraceNumber_02;
                payment.FunderTaxID = transaction.TRN_ReassociationTraceNumber.OriginatingCompanyIdentifier_03;

                payment.FunderName = GetRequiredString(_loop1000AId, loop1000A.N1_PayerIdentification.PremiumPayerName_02, "PremiumPayerName_02");
                payment.FunderID = loop1000A.N1_PayerIdentification.IntermediaryBankIdentifier_04;

                payment.Payee = GetRequiredString(_loop1000BId, loop1000B.N1_PayeeIdentification?.PremiumPayerName_02, "PremiumPayerName_02");
                payment.PayeeIdType = loop1000B.N1_PayeeIdentification?.IdentificationCodeQualifier_03; // XX=NPI, FI=TaxID
                payment.PayeeId = loop1000B.N1_PayeeIdentification?.IntermediaryBankIdentifier_04;

                payment.PayeeAddress1 = loop1000B.N3_PayeeAddress?.ResponseContactAddressLine_01;
                payment.PayeeAddress2 = loop1000B.N3_PayeeAddress?.ResponseContactAddressLine_02;
                payment.PayeeAddressCity = loop1000Bn4?.AdditionalPatientInformationContactCityName_01;
                payment.PayeeAddressState = loop1000Bn4?.AdditionalPatientInformationContactStateCode_02;
                payment.PayeeAddressZip = loop1000Bn4?.AdditionalPatientInformationContactPostalZoneorZIPCode_03;
                payment.PayeeAddressCountry = loop1000Bn4?.CountryCode_04;

                payment.FunderContactName = loop1000Aper.ResponseContactName_02;
                payment.FunderContactType = loop1000Aper.CommunicationNumberQualifier_03;
                payment.FunderContactInfo = loop1000Aper.ResponseContactCommunicationNumber_04;
                payment.TransactionNumber = curTransactionNum;
                payment.Status = PaymentStatus.Unapplied;
                payment.IsManualPayment = payment.PaymentEraUploadId != null ? true : false;
                payment.IsManualReconciled = false;

                var payeeAdditionalIdentification = loop1000B.REF_PayeeAdditionalIdentification?.FirstOrDefault();
                if ("TJ".Equals((payeeAdditionalIdentification?.ReferenceIdentificationQualifier_01 ?? "").ToUpper()))
                {
                    payment.PayeeTaxId = payeeAdditionalIdentification?.ReferenceIdentificationREF_02;

                }

                switch (payment.EraPaymentMethod)
                {
                    case "ACH":
                        payment.PaymentMethodId = (int)PaymentMethods.ACH;
                        break;
                    case "CHK":
                        payment.PaymentMethodId = (int)PaymentMethods.Check;
                        break;
                    case "NON": // For a "NON" payment, the method does not really matter
                        payment.PaymentMethodId = (int)PaymentMethods.NonPayment;
                        break;
                    default:
                        throw new EraEdiException($"Invalid payment method in BPR/BPR04={(PaymentMethods)payment.PaymentMethodId}. Only ACH, CHK, NON are valid.");
                }

                // capture originals 
                payment.PaymentAmountOrig = payment.PaymentAmount;

                var loop2000 = transaction.Loop2000;
                await ParseClaims(payment, loop2000);

            }
            catch (Exception ex)
            {
                var msg = $"{_fileID}/{payment.PaymentIdentifier}: Error parsing transaction # {curTransactionNum}. Error = {ex.Message}";
                var error = _paymentService.CreatePaymentError(payment,
                                                               msg,
                                                               PaymentErrorSeverity.Error,
                                                               EraErrorType.Parsing,
                                                               PaymentStatus.ParsingError);
                await _billingDbContext.PaymentErrors.AddAsync(error);
                LogError(msg, ex);
            }
        }

        private async Task ParseClaims(PaymentEntity payment, List<Loop_2000_835> loop2000)
        {
            if (loop2000.Count > 0)
            {
                LogMsg($"Adding {loop2000.Count} claims to payment {payment.PaymentIdentifier}");
                foreach (var loop2100 in loop2000.SelectMany(x => x.Loop2100))
                {
                    string claimIdentifier = "Unknown";
                    string clientIdentifier = "Unknown";
                    NM1_PatientName_2 nmClientSubmitted = null;

                    try
                    {

                        claimIdentifier = loop2100.CLP_ClaimPaymentInformation.PatientControlNumber_01 ??
                                          loop2100.AllREF?.REF_RenderingProviderIdentification.FirstOrDefault().ReferenceIdentificationREF_02;
                        if (string.IsNullOrWhiteSpace(claimIdentifier))
                        {
                            throw new EraEdiException("Missing claim identifier in Loop 2100.CLP.CLP01");
                        }

                        nmClientSubmitted = loop2100.AllNM1.NM1_PatientName;
                        if (nmClientSubmitted == null)
                        {
                            throw new EraEdiException("Missing client information in Loop 2100.NM1.NM01=QC");
                        }

                        clientIdentifier = GetRequiredString(_loop2100Id, nmClientSubmitted.ResponseContactIdentifier_09, "ResponseContactIdentifier_09");
                        if (string.IsNullOrWhiteSpace(clientIdentifier))
                        {
                            throw new EraEdiException("Missing client identifier in Loop 2100.NM1.NM109");
                        }

                    }
                    catch (Exception ex)
                    {
                        var msg = $"{_fileID}, Payment: {payment.PaymentIdentifier}. Error parsing claim: Missing or invalid claim identifier (CLP/CLP01). Error = {ex.Message}";
                        var error = _paymentService.CreatePaymentError(payment,
                                                                       msg,
                                                                       PaymentErrorSeverity.Error,
                                                                       EraErrorType.Parsing,
                                                                       PaymentStatus.ParsingError);
                        await _billingDbContext.PaymentErrors.AddAsync(error);
                        LogError(msg, ex);
                        continue;
                    }

                    try
                    {
                        var multiplier = 1;
                        var loop2100clp = loop2100.CLP_ClaimPaymentInformation;

                        var clmStatus = GetRequiredString(_loop2100Id, loop2100clp.ClaimStatusCode_02, "ClaimStatusCode_02");

                        if (clmStatus == "22")
                        {
                            // 22 = Reversal of Previous Payment
                            multiplier = -1;
                        }
                        var nmClientCorrected = loop2100.AllNM1.NM1_PatientName;
                        var nmRenderingProvider = loop2100.AllNM1.NM1_ServiceProviderName;
                        var dtmStart = GetDate(loop2100.AllDTM.DTM_StatementFromorToDate?.FirstOrDefault(x => x.DateTimeQualifier_01 == "232")?.Date_02, "dtmStart.Date_02");
                        var dtmEnd = GetDate(loop2100.AllDTM.DTM_StatementFromorToDate?.FirstOrDefault(x => x.DateTimeQualifier_01 == "233")?.Date_02, "dtmEnd.Date_02");
                        var dtmReceived = GetDate(loop2100.AllDTM.DTM_ClaimReceivedDate?.Date_02, "dtmReceived.Date_02");

                        // If the claim is denied, we need to update the ClaimStatus and ClaimHistory
                        var claimEntity = _billingDbContext.ClaimSubmissions.Where(cs => cs.ClaimSubmissionIdentifier == claimIdentifier && cs.DateDeleted == null)
                            .Select(cs => cs.Claim).FirstOrDefault();

                        #region Handle Denied and Reversal Logic
                        //Update the status and History Of Claims in DB #261908

                        if (claimEntity != null) {
                            UpdateClaimEntityOnDenied(clmStatus, claimEntity);
                        }

                        //Commenting as of now to test the Payment Posting Bug: 266632. and will also not keep the previous payment for denied claim.
                        #region handle prepayment and reversal payment logic

                        // If claim is denied then check if there is any prior payment or not, if yes then keep that  in totalPayment.
                        //decimal patientPayment = HandlePrepaymentOnDenied(claimIdentifier, clmStatus);

                        // Delete the Prepayment of Reversal Payment,
                        // DeletePreviousPaymentsOnReversalAndDenied(claimIdentifier, clmStatus);

                        #endregion

                        #endregion

                        // create PaymentClaim
                        PaymentClaimEntity paymentClaim = new PaymentClaimEntity()
                        {
                            Payment = payment,
                            PaymentId = payment.Id,
                            ClaimIdentifier = claimIdentifier,
                            ClaimId = claimEntity != null ? claimEntity.Id : 0,
                            ChildProfileId = claimEntity != null ? claimEntity.ChildProfileId : 0,
                            ClientIdentifier = clientIdentifier,
                            ClaimStatus = GetRequiredString(_loop2100Id, loop2100clp.ClaimStatusCode_02, "ClaimStatusCode_02"),
                            TotalCharge = GetRequiredDecimal(_loop2100Id, loop2100clp.TotalClaimChargeAmount_03, "TotalClaimChargeAmount_03"),
                            //TotalPayment = Convert.ToInt32(clmStatus) < 4 ? GetRequiredDecimal(_loop2100Id, loop2100clp.ClaimPaymentAmount_04, "ClaimPaymentAmount_04") * multiplier : patientPayment,
                            //TotalPayment = Convert.ToInt32(clmStatus) < 22 ? GetRequiredDecimal(_loop2100Id, loop2100clp.ClaimPaymentAmount_04, "ClaimPaymentAmount_04") * multiplier : 0,
                            TotalPayment = Convert.ToInt32(clmStatus) < 4 ? GetRequiredDecimal(_loop2100Id, loop2100clp.ClaimPaymentAmount_04, "ClaimPaymentAmount_04") * multiplier : 0,
                            PatientRespAmount = Convert.ToInt32(clmStatus) < 4 ? GetDecimalValue(loop2100clp.PatientResponsibilityAmount_05) : 0,
                            FilingIndicator = GetRequiredString(_loop2100Id, loop2100clp.ClaimFilingIndicatorCode_06, "ClaimFilingIndicatorCode_06"),
                            ControlNumber = GetRequiredString(_loop2100Id, loop2100clp.PayerClaimControlNumber_07, "PayerClaimControlNumber_07"),
                            ClientLastName = nmClientCorrected?.ResponseContactLastorOrganizationName_03 ??
                                                  GetRequiredString(_loop2100Id, nmClientSubmitted.ResponseContactLastorOrganizationName_03, "ResponseContactLastorOrganizationName_03"),
                            ClientFirstName = nmClientCorrected?.ResponseContactFirstName_04 ??
                                                  GetRequiredString(_loop2100Id, nmClientSubmitted.ResponseContactFirstName_04, "ResponseContactFirstName_04"),
                            ClientMiddleName = nmClientCorrected?.ResponseContactMiddleName_05 ??
                                                nmClientSubmitted.ResponseContactMiddleName_05,
                            PatientId = GetRequiredString(_loop2100Id, nmClientSubmitted.ResponseContactIdentifier_09, "ResponseContactIdentifier_09"),
                            ClaimReceivedDate = dtmReceived,
                            IsReviewed = false,
                            PlaceOfService = loop2100clp.FacilityTypeCode_08 != null ? GetRequiredString(_loop2100Id, loop2100clp.FacilityTypeCode_08, "FacilityTypeCode_08") : "",
                        };


                        if (nmRenderingProvider != null)
                        {
                            paymentClaim.RenderingProviderName = nmRenderingProvider.EntityTypeQualifier_02 == EntityTypeQualifier.NonPerson ?
                                nmRenderingProvider.ResponseContactLastorOrganizationName_03 :
                                    nmRenderingProvider.ResponseContactFirstName_04 + " " +
                                    (nmRenderingProvider.ResponseContactMiddleName_05 != null ? nmRenderingProvider.ResponseContactMiddleName_05 + " " : "") +
                                    nmRenderingProvider.ResponseContactLastorOrganizationName_03;
                            paymentClaim.RenderingProviderId = GetRequiredString(_loop2100Id, nmRenderingProvider.ResponseContactIdentifier_09, "ResponseContactIdentifier_09");
                        }

                        // copy originals
                        paymentClaim.ClaimIdentifierOrig = paymentClaim.ClaimIdentifier;
                        paymentClaim.ClaimStatusOrig = paymentClaim.ClaimStatus;
                        paymentClaim.TotalChargeOrig = paymentClaim.TotalCharge;
                        paymentClaim.TotalPaymentOrig = paymentClaim.TotalPayment;
                        paymentClaim.PatientRespAmountOrig = paymentClaim.PatientRespAmount;
                        paymentClaim.FilingIndicatorOrig = paymentClaim.FilingIndicator;

                        MarkCreated(paymentClaim, 0);

                        payment.PaymentClaims.Add(paymentClaim);

                        await CreateClaimCAS(paymentClaim, loop2100);
                        await ParseServiceLines(paymentClaim, loop2100);
                        if (dtmStart == null || dtmEnd == null)
                        {

                            dtmStart = paymentClaim.PaymentClaimServiceLines.Min(sl => sl.DateOfService ?? sl.ServiceStartDate);
                            dtmEnd = paymentClaim.PaymentClaimServiceLines.Min(sl => sl.DateOfService ?? sl.ServiceStartDate);

                        }
                        paymentClaim.ClaimDateFrom = dtmStart;
                        paymentClaim.ClaimDateTo = dtmEnd;

                        await UpdatePaymentFunderIdAsync(paymentClaim.ClaimId, payment);

                    }
                    catch (Exception ex)
                    {
                        var msg = $"{_fileID}, Payment: {payment.PaymentIdentifier}. Error parsing claim # {claimIdentifier}. Error = {ex.Message}";
                        var error = _paymentService.CreatePaymentError(payment,
                                                                       msg,
                                                                       PaymentErrorSeverity.Error,
                                                                       EraErrorType.Parsing,
                                                                       PaymentStatus.ParsingError);
                        await _billingDbContext.PaymentErrors.AddAsync(error);
                        LogError(msg, ex);
                    }

                }

            }
            else
            {
                var msg = $"Payment {payment.PaymentIdentifier} does not have specify any claim payments";
                var error = _paymentService.CreatePaymentError(payment,
                                                               msg,
                                                               PaymentErrorSeverity.Warning,
                                                               EraErrorType.Parsing);
                await _billingDbContext.PaymentErrors.AddAsync(error);
                LogWarn(msg);
            }
        }

        #region Handle Denied and Reversal Payment logic

        private decimal HandlePrepaymentOnDenied(string claimIdentifier, string clmStatus)
        {
            var patientPayment = Convert.ToDecimal(0.00);
            if (clmStatus.Trim() == "4")
            {
                // Get the PaymentClaim from ClaimIdentifier
                var paymentClaimEntity = _billingDbContext.PaymentClaims
                    .Where(cs => cs.ClaimIdentifier.Trim() == claimIdentifier.Substring(0, claimIdentifier.Length - 1).Trim()
                        && cs.DateDeleted == null && cs.ClaimStatus.Trim() == "1").ToList();
                if (paymentClaimEntity.Count > 0)
                {
                    // If there are multiple payments for the same claim, sum them up
                    patientPayment = paymentClaimEntity.Sum(x => x.TotalPayment) ?? 0;
                }
            }

            return patientPayment;
        }

        private void DeletePreviousPaymentsOnReversalAndDenied(string claimIdentifier, string clmStatus)
        {
            if (clmStatus.Trim() == "22" || clmStatus.Trim() == "4")
            {
                // Get the PaymentClaim from ClaimIdentifier
                var paymentClaimEntities = _billingDbContext.PaymentClaims
                    .Where(cs => cs.ClaimIdentifier.Trim() == claimIdentifier.Substring(0, claimIdentifier.Length - 1).Trim()
                        && cs.DateDeleted == null && cs.ClaimStatus.Trim() == "1").ToList();
                if (paymentClaimEntities.Count > 0)
                {
                    // Delete these entries in the PaymentClaim
                    foreach (var paymentClaim in paymentClaimEntities)
                    {
                        paymentClaim.DateDeleted = EstDateTime;
                        MarkUpdated(paymentClaim, 0);
                        _billingDbContext.PaymentClaims.AddOrUpdateById(paymentClaim);
                        // Also delete the Service Lines associated with this PaymentClaim
                        var serviceLines = _billingDbContext.PaymentClaimServiceLines
                            .Where(s => s.PaymentClaimId == paymentClaim.Id && s.DateDeleted == null).ToList();
                        foreach (var serviceLine in serviceLines)
                        {
                            serviceLine.DateDeleted = EstDateTime;
                            MarkUpdated(serviceLine, 0);
                            _billingDbContext.PaymentClaimServiceLines.AddOrUpdateById(serviceLine);
                        }
                    }
                }
            }
        }

        private void UpdateClaimEntityOnDenied(string clmStatus, ClaimEntity claimEntity)
        {
            var statusToUpdate = new List<int> { 1, 2, 3, 4 };

            if (statusToUpdate.Contains(Convert.ToInt32(clmStatus)))
            {
                var status = clmStatus == "4" ? ClaimStatus.Denied : ClaimStatus.Closed;
                claimEntity.ClaimStatus = status;
                MarkUpdated(claimEntity, 0);
                _billingDbContext.Claims.AddOrUpdateById(claimEntity);

                // Update the ClaimHistory as well
                var historyEntry = new ClaimHistoryEntity()
                {
                    ClaimId = claimEntity.Id,
                    Mode = ClaimActionMode.System,
                    ClaimAction = ClaimAction.PaymentPosted,
                    ClaimHistoryAction = ClaimHistoryAction.DeniedByPayer,
                    ActionDate = EstDateTime,
                    OldValue = claimEntity.ClaimStatus.ToString() ?? string.Empty,
                    NewValue = ((ClaimStatus)status).ToString(),
                };

                MarkCreated(historyEntry, 0);

                _billingDbContext.ClaimHistory.AddOrUpdateById(historyEntry);
            }
        }

        #endregion

        private async Task ParseServiceLines(PaymentClaimEntity paymentClaim, Loop_2100_835 loop2100)
        {
            var loop2110s = loop2100.Loop2110;
            if (loop2110s != null && loop2110s.Count > 0)
            {
                var curServiceLineIdx = 1;
                foreach (var loop2110 in loop2110s)
                {
                    try
                    {
                        var status = 0;
                        int.TryParse(paymentClaim.ClaimStatus, out status);

                        var dtmDOS = loop2110?.DTM_ServiceDate?.FirstOrDefault(x => x.DateTimeQualifier_01 == "472");
                        var dtmStart = loop2100?.AllDTM?.DTM_StatementFromorToDate?.FirstOrDefault(x => x.DateTimeQualifier_01 == "232"); //loop2110.DTM_ServiceDate.FirstOrDefault(x => x.DateTimeQualifier_01 == "232");
                        var dtmEnd = loop2100?.AllDTM?.DTM_StatementFromorToDate?.FirstOrDefault(x => x.DateTimeQualifier_01 == "233");//loop2110.DTM_ServiceDate.FirstOrDefault(x => x.DateTimeQualifier_01 == "233");

                        var amtAllowed = loop2110.AMT_ServiceSupplementalAmount?.FirstOrDefault(x => x.AmountQualifierCode_01 == "B6");
                        var loop2110svc = loop2110.SVC_ServicePaymentInformation;
                        var loop2110lq = loop2110?.LQ_HealthCareRemarkCodes?.FirstOrDefault();

                        // create claim
                        PaymentClaimServiceLineEntity serviceLine = new PaymentClaimServiceLineEntity();
                        serviceLine.PaymentClaim = paymentClaim;
                        serviceLine.PaymentClaimId = paymentClaim.Id;
                        serviceLine.ServiceCode = loop2110svc.CompositeMedicalProcedureIdentifier_01.ProcedureCode_02;
                        serviceLine.ChargeAmount = Convert.ToDecimal(loop2110svc.LineItemChargeAmount_02);
                        //serviceLine.PaymentAmount = status < 22 ? Convert.ToDecimal(loop2110svc.MonetaryAmount_03) : paymentClaim.TotalPayment;
                        serviceLine.PaymentAmount = status < 4 ? Convert.ToDecimal(loop2110svc.MonetaryAmount_03) : paymentClaim.TotalPayment;
                        serviceLine.ProcedureModifier1 = loop2110svc.CompositeMedicalProcedureIdentifier_01.ProcedureModifier_03;
                        serviceLine.ProcedureModifier2 = loop2110svc.CompositeMedicalProcedureIdentifier_01.ProcedureModifier_04;
                        serviceLine.ProcedureModifier3 = loop2110svc.CompositeMedicalProcedureIdentifier_01.ProcedureModifier_05;
                        serviceLine.ProcedureModifier4 = loop2110svc.CompositeMedicalProcedureIdentifier_01.ProcedureModifier_06;
                        serviceLine.ProcedureDesc = loop2110svc.CompositeMedicalProcedureIdentifier_01.Description_07;
                        serviceLine.ProcedureUnits = loop2110svc.Quantity_05;
                        serviceLine.ReplacementServiceCode = loop2110svc?.CompositeMedicalProcedureIdentifier_06?.ProcedureCode_02;
                        serviceLine.ReplacementProcedureModifier1 = loop2110svc?.CompositeMedicalProcedureIdentifier_06?.ProcedureModifier_03;
                        serviceLine.ReplacementProcedureModifier2 = loop2110svc?.CompositeMedicalProcedureIdentifier_06?.ProcedureModifier_04;
                        serviceLine.ReplacementProcedureModifier3 = loop2110svc?.CompositeMedicalProcedureIdentifier_06?.ProcedureModifier_05;
                        serviceLine.ReplacementProcedureModifier4 = loop2110svc?.CompositeMedicalProcedureIdentifier_01.ProcedureModifier_06;
                        serviceLine.ReplacementProcedureUnits = loop2110svc.CompositeMedicalProcedureIdentifier_01.Description_07;
                        serviceLine.ReplacementProcedureDesc = loop2110svc.UnitsofServiceCount_07;
                        serviceLine.DateOfService = dtmDOS != null ? GetDate(dtmDOS.Date_02, "dtmDOS.Date_02") : GetDate(dtmStart.Date_02, "dtmStart.Date_02");

                        var chargeEntry = _billingDbContext.ClaimChargeEntries.Where(x => x.ClaimId == paymentClaim.ClaimId && x.BillingCode == serviceLine.ServiceCode && x.DateOfService == serviceLine.DateOfService && x.Charges == serviceLine.ChargeAmount).FirstOrDefault();
                        serviceLine.ClaimChargeEntryId = chargeEntry?.Id;

                        serviceLine.ServiceStartDate = GetDate(dtmStart?.Date_02, "dtmStart?.Date_02");
                        serviceLine.ServiceEndDate = GetDate(dtmEnd?.Date_02, "dtmEnd?.Date_02");
                        serviceLine.LineControlNumber = loop2110.AllREF?.REF_RenderingProviderInformation?.FirstOrDefault().ReferenceIdentificationREF_02;
                        serviceLine.AllowedAmount = status < 4 ? GetDecimalValue(amtAllowed?.TotalClaimChargeAmount_02) : 0;

                        if (loop2110lq != null)
                        {
                            serviceLine.RemittanceRemarkCode1 = loop2110lq.CodeListQualifierCode_01;
                            serviceLine.RemittanceRemarkCode2 = loop2110lq.FormIdentifier_02;
                            serviceLine.RemittanceRemarkCode1Orig = serviceLine.RemittanceRemarkCode1;
                            serviceLine.RemittanceRemarkCode2Orig = serviceLine.RemittanceRemarkCode2;
                        }

                        // copy originals
                        serviceLine.ServiceCodeOrig = serviceLine.ServiceCode;
                        serviceLine.ChargeAmountOrig = serviceLine.ChargeAmount;
                        serviceLine.PaymentAmountOrig = serviceLine.PaymentAmount;
                        serviceLine.ProcedureModifier1Orig = serviceLine.ProcedureModifier1;
                        serviceLine.ProcedureModifier2Orig = serviceLine.ProcedureModifier2;
                        serviceLine.ProcedureModifier3Orig = serviceLine.ProcedureModifier3;
                        serviceLine.ProcedureModifier4Orig = serviceLine.ProcedureModifier4;
                        serviceLine.ProcedureUnitsOrig = serviceLine.ProcedureUnits;
                        serviceLine.DateOfServiceOrig = serviceLine.DateOfService;
                        serviceLine.ServiceStartDateOrig = serviceLine.ServiceStartDate;
                        serviceLine.ServiceEndDateOrig = serviceLine.ServiceEndDate;
                        serviceLine.AllowedAmountOrig = serviceLine.AllowedAmount;


                        MarkCreated(serviceLine, 0);

                        paymentClaim.PaymentClaimServiceLines.Add(serviceLine);
                        curServiceLineIdx += 1;
                        if (int.TryParse(paymentClaim.ClaimStatus, out int claimStatusValue))
                        {
                            await CreateServiceLineCAS(serviceLine, loop2110, claimStatusValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        var msg = $"{_fileID}/{paymentClaim.Payment?.PaymentIdentifier}/{paymentClaim.ClaimIdentifier}. Error parsing service line # {curServiceLineIdx}. Error = {ex.Message}";
                        var error = _paymentService.CreatePaymentError(paymentClaim.Payment,
                                                                       msg,
                                                                       PaymentErrorSeverity.Error,
                                                                       EraErrorType.Parsing,
                                                                       PaymentStatus.ParsingError);
                        await _billingDbContext.PaymentErrors.AddAsync(error);
                        LogError(msg, ex);
                    }
                }

            }
        }

        private async Task CreateClaimCAS(PaymentClaimEntity claimPmt, Loop_2100_835 loop2100)
        {
            var casList = loop2100.CAS_ClaimsAdjustment;
            if (casList != null && casList.Count > 0)
            {
                foreach (var cas in casList)
                {
                    try
                    {
                        var adjustment = new PaymentClaimAdjustmentEntity()
                        {
                            PaymentClaim = claimPmt,
                            PaymentClaimId = claimPmt.Id,
                            AdjustmentGroupCode = GetRequiredString(_loop2110Id, cas.ClaimAdjustmentGroupCode_01, "ClaimAdjustmentGroupCode_01"),
                            AdjustmentReasonCode = GetRequiredString(_loop2110Id, cas.AdjustmentReasonCode_02, "AdjustmentReasonCode_02"),
                            AdjustmentAmount = GetRequiredDecimal(_loop2110Id, cas.AdjustmentAmount_03, "AdjustmentAmount_03"),
                            AdjustmentQuantity = GetDecimalValue(cas.AdjustmentQuantity_04),
                        };
                        // capture original values
                        adjustment.AdjustmentGroupCodeOrig = adjustment.AdjustmentGroupCode;
                        adjustment.AdjustmentReasonCodeOrig = adjustment.AdjustmentReasonCode;
                        adjustment.AdjustmentAmountOrig = adjustment.AdjustmentAmount;
                        adjustment.AdjustmentQuantityOrig = adjustment.AdjustmentQuantity;

                        MarkCreated(adjustment, 0);

                        claimPmt.PaymentClaimAdjustments.Add(adjustment);
                    }
                    catch (Exception ex)
                    {
                        var msg = $"{_fileID}/" +
                                  $"{claimPmt.Payment.PaymentIdentifier}/" +
                                  $"{claimPmt.ClaimIdentifier}: Error parsing claim level adjustment (CAS). Error = {ex.Message}";
                        var error = _paymentService.CreatePaymentError(claimPmt.Payment,
                                                                       msg,
                                                                       PaymentErrorSeverity.Error,
                                                                       EraErrorType.Parsing,
                                                                       PaymentStatus.ParsingError);
                        await _billingDbContext.PaymentErrors.AddAsync(error);
                        LogError(msg, ex);
                    }
                }
            }
        }

        private async Task CreateServiceLineCAS(PaymentClaimServiceLineEntity serviceLine, Loop_2110_835 loop2110, int claimStatus)
        {
            var casList = loop2110.CAS_ServiceAdjustment;
            if (casList != null)
            {
                foreach (var cas in casList)
                {
                    try
                    {
                        // Only add the GroupCode and Amount if the claim status is less than 4 (i.e. not denied or reversed)
                        var adjustment = new PaymentClaimServiceLineAdjustmentEntity()
                        {
                            PaymentClaimServiceLine = serviceLine,
                            PaymentClaimServiceLineId = serviceLine.Id,
                            AdjustmentGroupCode = GetRequiredString(_loop2110Id, cas.ClaimAdjustmentGroupCode_01, "ClaimAdjustmentGroupCode_01"),
                            AdjustmentReasonCode = GetRequiredString(_loop2110Id, cas.AdjustmentReasonCode_02, "AdjustmentReasonCode_02"),
                            AdjustmentAmount = claimStatus < 4 ? GetRequiredDecimal(_loop2110Id, cas.AdjustmentAmount_03, "AdjustmentAmount_03") : 0,
                            AdjustmentQuantity = GetDecimalValue(cas.AdjustmentQuantity_04),
                        };
                        // capture original values
                        adjustment.AdjustmentGroupCodeOrig = adjustment.AdjustmentGroupCode;
                        adjustment.AdjustmentReasonCodeOrig = adjustment.AdjustmentReasonCode;
                        adjustment.IsAdjustmentPositive = false;
                        adjustment.Mode = ClaimActionMode.System;

                        // By deafualt all adjustments received from ERA are considered as negative so below code is to save positive adjustment
                        if (adjustment.AdjustmentAmount < 0 && claimStatus < 4)
                        {
                            adjustment.AdjustmentAmount = adjustment.AdjustmentAmount - (2 * adjustment.AdjustmentAmount);
                            adjustment.IsAdjustmentPositive = true;
                        }
                        adjustment.AdjustmentAmountOrig = claimStatus < 4 ? adjustment.AdjustmentAmount : 0;
                        adjustment.AdjustmentQuantityOrig = adjustment.AdjustmentQuantity;

                        MarkCreated(adjustment, 0);

                        serviceLine.PaymentClaimServiceLineAdjustments.Add(adjustment);

                    }
                    catch (Exception ex)
                    {
                        var msg = $"{_fileID}/" +
                                  $"{serviceLine.PaymentClaim.Payment.PaymentIdentifier}/" +
                                  $"{serviceLine.PaymentClaim.ClaimIdentifier}/" +
                                  $"{serviceLine.DateOfService}/" +
                                  $"{serviceLine.ServiceCode}: Error parsing service line adjustment (CAS). Error = {ex.Message}";
                        var error = _paymentService.CreatePaymentError(serviceLine.PaymentClaim.Payment,
                                                                       msg,
                                                                       PaymentErrorSeverity.Error,
                                                                       EraErrorType.Parsing,
                                                                       PaymentStatus.ParsingError);
                        await _billingDbContext.PaymentErrors.AddAsync(error);
                        LogError(msg, ex);
                    }
                }
            }
        }

        private string GetRequiredString(string identifier, string value, string elementName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new EraEdiException($"Missing value [{identifier} ({elementName})]");
            }

            return value;
        }

        private int GetRequiredInt(string identifier, string value, string elementName)
        {
            return Convert.ToInt32(GetRequiredString(identifier, value, elementName));
        }

        private bool GetBoolValue(string value)
        {
            return value?.Equals("1") ?? false;
        }

        private decimal GetRequiredDecimal(string identifier, string value, string name)
        {
            return Convert.ToDecimal(GetRequiredString(identifier, value, name));
        }

        private decimal GetDecimalValue(string value, decimal defaultValue = 0m)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return Convert.ToDecimal(value);
        }

        private DateTime? GetDate(string value, string fieldname)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var result = DateTimeExt.FromEdiString(value);
            if (result == DateTime.MinValue)
            {
                throw new Exception($"Invalid date value for {fieldname}: {(value ?? "(missing)")}");
            }

            return result;
        }

        private void LogDebug(string msg)
        {
            var msgToWrite = $"[{DateTime.Now:G}]: {msg}";
            _logger.LogInformation(msgToWrite);
        }

        private void LogMsg(string msg)
        {
            var msgToWrite = $"[{DateTime.Now:G}]: {msg}";
            _logger.LogInformation(msgToWrite);
        }

        private void LogWarn(string msg)
        {
            var msgToWrite = $"[{DateTime.Now:G}]: {msg}";
            _logger.LogWarning(msgToWrite);
        }

        private void LogError(string msg, Exception ex = null)
        {
            LastError = new EraParserError()
            {
                Message = msg,
                Exception = ex
            };
            var msgToWrite = $"[{DateTime.Now:G}]: {msg}";
            _logger.LogError(msgToWrite);
        }

        private async Task UpdatePaymentFunderIdAsync(int? claimId, PaymentEntity payment)
        {
            try
            {

                if (claimId == null)
                {
                    LogWarn("ClaimId is null. Skipping update.");
                    return;
                }

                var latestFunderId = await _billingDbContext.ClaimSubmissions
                    .Where(cs => cs.ClaimId == claimId && cs.DateDeleted == null)
                    .OrderByDescending(cs => cs.DateCreated)
                    .Select(cs => cs.FunderId)
                    .FirstOrDefaultAsync();

                if (latestFunderId == null)
                {
                    LogWarn($"No FunderId found for ClaimId: {claimId}. Skipping update.");
                    return;
                }

                payment.FunderID = Convert.ToString(latestFunderId);    //need to see, if its coming from actual ERA file then dont need to update it here.
                payment.HcFunderId = latestFunderId;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating FunderID for ClaimId: {claimId} and PaymentId: {payment.Id}. Error: {ex.Message}";
                LogError(errorMessage, ex);
            }
        }
    }
}
