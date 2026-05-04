using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Interfaces;
using System;
using System.Net.Mime;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Payment")]
    [Route("[controller]/[action]")]
    public class PaymentPostingController : Controller
    {
        private readonly IPaymentPostingService _paymentPostingService;
        private readonly IFunderService _funderService;
        private readonly IChildProfileService _childProfileService;
        private readonly ILogger<PaymentPostingController> _logger;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;

        public PaymentPostingController(IPaymentPostingService paymentPostingService, IFunderService funderService,
            IChildProfileService childProfileService, ILogger<PaymentPostingController> logger,
            IRethinkMasterDataMicroServices rethinkServices)
        {
            _paymentPostingService = paymentPostingService;
            _funderService = funderService;
            _childProfileService = childProfileService;
            _logger = logger;
            _rethinkServices = rethinkServices;
        }

        [HttpPost]
        public async Task<IActionResult> ManualCreatePayment([FromBody] ManualCreatePaymentModelRequest patientPaymentModel)
        {
            _logger.LogInformation("{Controller}.{Action} - ManualCreatePayment called. AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    nameof(PaymentPostingController),
                    nameof(ManualCreatePayment),
                    patientPaymentModel?.AccountInfoId,
                    patientPaymentModel?.MemberId);
            try
            {
                var result = await _paymentPostingService.CreateManualPatientPaymentAsync(patientPaymentModel);
                if (patientPaymentModel.UnAllocatedAmount != null || !string.IsNullOrWhiteSpace(patientPaymentModel.Notes) && result != 0)
                {
                    var unAllocatedPaymentsModel = new UnAllocatedPaymentsModel
                    {
                        AccountInfoId = patientPaymentModel.AccountInfoId,
                        MemberId = patientPaymentModel.MemberId,
                        PaymentId = result,
                        ChildProfileId = patientPaymentModel.PatientId ?? 0,
                        UnAllocatedAmount = patientPaymentModel.UnAllocatedAmount ?? 0,
                        Notes = patientPaymentModel.Notes
                    };

                    await _paymentPostingService.AddUnAllocatedPayments(unAllocatedPaymentsModel);
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(ManualCreatePayment)} - ManualCreatePayment failed. ErrorMsg ={e.Message}");
                BadRequest(e);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPayments([FromBody] GetPaymentsModel getPaymentsModel)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPayments called. AccountInfoId={AccountInfoId}, MemberId={MemberId}, Skip={Skip}, Take={Take}",
                nameof(PaymentPostingController),
                nameof(GetPayments),
                getPaymentsModel.AccountInfoId,
                getPaymentsModel.MemberId,
                getPaymentsModel.Skip,
                getPaymentsModel.Take);

            try
            {
                var result = await _paymentPostingService.GetAllPayments(getPaymentsModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetPayments)} -GetPayments failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetFunders([FromBody] FunderSearchModelWithUserInfo funderSearchModel)
        {
            _logger.LogInformation("{Controller}.{Action} - GetFunders called. AccountInfoId={AccountInfoId}, FunderName={FunderName}, Skip={Skip}, Take={Take}",
                nameof(PaymentPostingController),
                nameof(GetFunders),
                funderSearchModel.AccountInfoId,
                funderSearchModel.FunderName,
                funderSearchModel.Skip,
                funderSearchModel.Take);

            try
            {
                var result = await _funderService.GetFundersAsync(funderSearchModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetFunders)} -GetFunders failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAssignedFunders([FromBody] FunderSearchModelWithUserInfo funderSearchModel)
        {
            _logger.LogInformation("{Controller}.{Action} - GetAssignedFunders called. AccountInfoId={AccountInfoId}, FunderName={FunderName}, Skip={Skip}, Take={Take}",
                nameof(PaymentPostingController),
                nameof(GetAssignedFunders),
                funderSearchModel.AccountInfoId,
                funderSearchModel.FunderName,
                funderSearchModel.Skip,
                funderSearchModel.Take);

            try
            {
                var result = await _paymentPostingService.GetAssignedFundersAsync(funderSearchModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetAssignedFunders)} -GetAssignedFunders failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult GetPaymentMethods()
        {
            _logger.LogInformation("{Controller}.{Action} - GetPaymentMethods called.",
                nameof(PaymentPostingController),
                nameof(GetPaymentMethods));

            try
            {
                var result = _paymentPostingService.GetPaymentMethods();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetPaymentMethods)} -GetPaymentMethods failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult GetReconcileStatuses()
        {
            _logger.LogInformation("{Controller}.{Action} - GetReconcileStatuses called.",
                nameof(PaymentPostingController),
                nameof(GetReconcileStatuses));

            try
            {
                var result = _paymentPostingService.GetReconcileStatuses();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetReconcileStatuses)} -GetReconcileStatuses failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentSummary([FromBody] int paymentId)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPaymentSummary called. PaymentId={PaymentId}",
                nameof(PaymentPostingController),
                nameof(GetPaymentSummary),
                paymentId);

            try
            {
                var result = await _paymentPostingService.GetPaymentSummaryAsync(paymentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetPaymentSummary)} -GetPaymentSummary failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateManualPaymentSummary([FromBody] UpdateManualPaymentSummary model)
        {
            _logger.LogInformation("{Controller}.{Action} - UpdateManualPaymentSummary called. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                nameof(PaymentPostingController),
                nameof(UpdateManualPaymentSummary),
                model.Id,
                model.AccountInfoId,
                model.MemberId);

            try
            {
                await _paymentPostingService.UpdateManualPaymentSummaryAsync(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(UpdateManualPaymentSummary)} -UpdateManualPaymentSummary failed. PaymentId={model.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePaymentSummary([FromBody] UpdatePaymentSummary model)
        {
            _logger.LogInformation("{Controller}.{Action} - UpdatePaymentSummary called. PaymentId={PaymentId}",
                nameof(PaymentPostingController),
                nameof(UpdatePaymentSummary),
                model.Id);

            try
            {
                await _paymentPostingService.UpdatePaymentSummaryAsync(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(UpdatePaymentSummary)} -UpdatePaymentSummary failed. PaymentId={model.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentShortInfo([FromBody] int paymentId)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPaymentShortInfo called. PaymentId={PaymentId}",
                nameof(PaymentPostingController),
                nameof(GetPaymentShortInfo),
                paymentId);

            try
            {
                var result = await _paymentPostingService.GetPaymentShortInfoAsync(paymentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetPaymentShortInfo)} -GetPaymentShortInfo failed. PaymentId={paymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPatients([FromBody] PersonSearchModel personSearchModel)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPatients called. AccountInfoId={AccountInfoId}, PersonName={PersonName}",
                nameof(PaymentPostingController),
                nameof(GetPatients),
                personSearchModel.AccountInfoId,
                personSearchModel.PersonName);

            try
            {
                var result = await _childProfileService.GetAccountPatinetsByNameAsync(personSearchModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetPatients)} -GetPatients failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePayment([FromBody] UpdatePaymentModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - DeletePayment called. PaymentIds={PaymentIds}, MemberId={MemberId}",
                nameof(PaymentPostingController),
                nameof(DeletePayment),
                string.Join(",", model.PaymentId ?? new int[0]),
                model.MemberId);

            try
            {
                var result = await _paymentPostingService.DeletePaymentAsync(model.PaymentId, model.MemberId, model.AccountInfoId);

                _logger.LogInformation("{Controller}.{Action} - DeletePayment processed. PaymentIds={PaymentIds}",
                    nameof(PaymentPostingController),
                    nameof(DeletePayment),
                    string.Join(",", model.PaymentId ?? new int[0]));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(DeletePayment)} -DeletePayment failed. PaymentIds={string.Join(",", model.PaymentId ?? new int[0])}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> ReconcilePayment([FromBody] UpdatePaymentModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - ReconcilePayment called. PaymentIds={PaymentIds}, MemberId={MemberId}",
                nameof(PaymentPostingController),
                nameof(ReconcilePayment),
                string.Join(",", model.PaymentId ?? new int[0]),
                model.MemberId);

            try
            {
                var result = await _paymentPostingService.ReconcilePaymentAsync(
                    model.PaymentId,
                    model.MemberId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(ReconcilePayment)} -ReconcilePayment failed. PaymentIds={string.Join(",", model.PaymentId ?? new int[0])}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReconcileClaim([FromBody] ClaimPaymentUpdateModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - ReconcileClaim called. PaymentIds={PaymentIds}, ClaimId={ClaimId}, MemberId={MemberId}",
                nameof(PaymentPostingController),
                nameof(ReconcileClaim),
                string.Join(",", model.PaymentId ?? new int[0]),
                model.ClaimId,
                model.MemberId);

            try
            {
                var result = await _paymentPostingService.ReconcileClaimAsync(
                    model.PaymentId,
                    model.ClaimId,
                    model.MemberId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(ReconcileClaim)} -ReconcileClaim failed. PaymentIds={string.Join(",", model.PaymentId ?? new int[0])}, ClaimId={model.ClaimId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEOBPaymentInfo([FromBody] int paymentId)
        {
            _logger.LogInformation("{Controller}.{Action} - GetEOBPaymentInfo called. PaymentId={PaymentId}",
                nameof(PaymentPostingController),
                nameof(GetEOBPaymentInfo),
                paymentId);

            try
            {
                var result = await _paymentPostingService.GetEOBPaymentInfoAsync(paymentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetEOBPaymentInfo)} -GetEOBPaymentInfo failed. PaymentId={paymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetNextPaymentID([FromBody] UserInfo userInfo)
        {
            _logger.LogInformation("{Controller}.{Action} - GetNextPaymentID called. AccountInfoId={AccountInfoId}",
                nameof(PaymentPostingController),
                nameof(GetNextPaymentID),
                userInfo.AccountInfoId);

            try
            {
                string nextPaymentID = await _paymentPostingService.GetNextPaymentID(userInfo.AccountInfoId);
                return Json(nextPaymentID);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetNextPaymentID)} -GetNextPaymentID failed. ErrorMsg ={ex.Message} ");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromBody] EraUploadModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - UploadFile called. AccountInfoId={AccountInfoId}, FileName={FileName}",
                nameof(PaymentPostingController),
                nameof(UploadFile),
                model.AccountInfoId,
                model.FileName);

            try
            {
                var result = await _paymentPostingService.UploadFileAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(UploadFile)} -UploadFile failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUpload([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - DeleteUpload called. UploadId={UploadId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                nameof(PaymentPostingController),
                nameof(DeleteUpload),
                model.Id,
                model.AccountInfoId,
                model.MemberId);

            try
            {
                await _paymentPostingService.DeleteUploadAsync(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(DeleteUpload)} -DeleteUpload failed. UploadId={model.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetProcessingPayments([FromBody] UserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetProcessingPayments called. AccountInfoId={AccountInfoId}",
                nameof(PaymentPostingController),
                nameof(GetProcessingPayments),
                model.AccountInfoId);

            try
            {
                var result = await _paymentPostingService.GetProcessingPaymentsAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetProcessingPayments)} -GetProcessingPayments failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> StartPaymentParsing([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - StartPaymentParsing called. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}",
                nameof(PaymentPostingController),
                nameof(StartPaymentParsing),
                model.Id,
                model.AccountInfoId);

            try
            {
                await _paymentPostingService.StartPaymentParsingAsync(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(StartPaymentParsing)} -StartPaymentParsing failed. PaymentId={model.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> HideProcessingInfo([FromBody] HideProcessingInfoModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - HideProcessingInfo called. PaymentIds={PaymentIds}, AccountInfoId={AccountInfoId}",
                nameof(PaymentPostingController),
                nameof(HideProcessingInfo),
                string.Join(",", model.PaymentIds ?? new System.Collections.Generic.List<int>()),
                model.AccountInfoId);

            try
            {
                await _paymentPostingService.HideProcessingInfoAsync(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(HideProcessingInfo)} -HideProcessingInfo failed. PaymentIds={string.Join(",", model.PaymentIds ?? new System.Collections.Generic.List<int>())}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetFileUpload([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetFileUpload called. UploadId={UploadId}",
                nameof(PaymentPostingController),
                nameof(GetFileUpload),
                model.Id);

            try
            {
                var attachmentReturnModel = await _paymentPostingService.GetUploadAsync(model);
                return File(attachmentReturnModel.MemoryStream,
                    MediaTypeNames.Application.Octet,
                    attachmentReturnModel.Filename);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetFileUpload)} -GetFileUpload failed. UploadId={model.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Adding and Updating UnAllocated payments based on PaymentId
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddUnAllocatedPayments([FromBody] UnAllocatedPaymentsModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - AddUnAllocatedPayments called. MemberId={MemberId}, PaymentId={PaymentId}",
                nameof(PaymentPostingController),
                nameof(AddUnAllocatedPayments),
                model.MemberId,
                model.PaymentId);

            try
            {
                await _paymentPostingService.AddUnAllocatedPayments(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(AddUnAllocatedPayments)} -Error in adding UnAllocated Payment : memberId={model.MemberId}, Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the Unallocated Payment details based on PaymentId and ChildProfileId.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetUnAllocatedPayments([FromBody] UnAllocatedPaymentRequestModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetUnAllocatedPayments called. MemberId={MemberId}, PaymentId={PaymentId}, ChildProfileId={ChildProfileId}",
                nameof(PaymentPostingController),
                nameof(GetUnAllocatedPayments),
                model.MemberId,
                model.PaymentId,
                model.ChildProfileId);

            try
            {
                var unAllocatedPaymentResult = await _paymentPostingService.GetUnAllocatedPaymentsById(model);
                return Ok(unAllocatedPaymentResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetUnAllocatedPayments)} -Error in fetching UnAllocated Payments : memberId={model.MemberId}, Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the Gurantor Details by AccountInfoId and ClientId.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetGuarantorDetailsById([FromBody] ClientHistoryUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetGuarantorDetailsById called. MemberId={MemberId}, AccountInfoId={AccountInfoId}, ClientId={ClientId}",
                nameof(PaymentPostingController),
                nameof(GetGuarantorDetailsById),
                model?.MemberId,
                model?.AccountInfoId,
                model?.ClientId);

            try
            {
                var gurantorData = await _paymentPostingService.GetGuarantorDetailsById(model);
                return Ok(gurantorData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentPostingController)}.{nameof(GetGuarantorDetailsById)} -Error in fetching UnAllocated Payments : memberId={model.MemberId}, Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRevSpringPayload([FromBody] RevSpringPayloadRequestModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetRevSpringPayload called. AccountInfoId={AccountInfoId}, MemberId={MemberId}, ClientId={ClientId}",
                nameof(PaymentPostingController),
                nameof(GetRevSpringPayload),
                model?.AccountInfoId,
                model?.MemberId,
                model?.ClientId);

            try
            {
                if (model == null) return BadRequest("Request body is required.");
                if (model.AccountInfoId <= 0) return BadRequest("AccountInfoId is required.");
                if (model.MemberId <= 0) return BadRequest("MemberId is required.");
                if (model.ClientId <= 0) return BadRequest("ClientId is required.");
                if (model.AmountDue <= 0) return BadRequest("AmountDue must be > 0.");
                if (string.IsNullOrWhiteSpace(model.UserEmail)) return BadRequest("UserEmail is required.");


                var guarantorDetails = await _paymentPostingService.GetGuarantorDetailsById(new ClientHistoryUserInfo
                {
                    AccountInfoId = model.AccountInfoId,
                    MemberId = model.MemberId,
                    ClientId = model.ClientId
                });

                var patientDetails = await _paymentPostingService.GetPatientAccountDetails(model.AccountInfoId, model.ClientId);

                if (guarantorDetails == null)
                    return BadRequest("Guarantor details not found.");
                if (patientDetails == null)
                    return BadRequest("Patient details not found.");

                var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(model.AccountInfoId, false);
                string orgSiteName = string.Empty;
                if (accountInfo?.subscriptionFeatures != null &&
                    accountInfo.subscriptionFeatures.ContainsKey(CommonService.revSpringOrgSiteId))

                {
                    orgSiteName = accountInfo.subscriptionFeatures[CommonService.revSpringOrgSiteId].ToString();
                }

                // if state id is not null in guarantor details,
                // then fetch the state details from rethink masterdata service and set the state name in guarantor details

                if (guarantorDetails?.Address?.StateId != null)
                {
                    var state = await _rethinkServices.GetStateById(guarantorDetails.Address.StateId.Value);

                    if (state != null && !string.IsNullOrWhiteSpace(state.abbreviation))
                    {
                        guarantorDetails.Address.State = state.abbreviation;
                    }
                }

                var payloadResponse = new RevSpringPayloadResponse
                {
                    Payload = new RevSpringPayload
                    {
                        ConsumerNumber = guarantorDetails.Id.ToString(),
                        ExternalUsername = model.UserEmail,
                        UserEmail = model.UserEmail,
                        UserLastName = model.UserLastName,
                        RoleName = "CSR",
                        AccessLevel = "Org",
                        MemberId = model.MemberId,
                        AccountId = model.AccountInfoId,
                        ReferenceNo = model.ReferenceNo,
                        PatientId = model.ClientId.ToString(),
                        PatientFirstName = patientDetails?.FirstName ?? string.Empty,
                        PatientLastName = patientDetails?.LastName ?? string.Empty,
                        OrgSiteName = orgSiteName,
                        DataContext = new RevSpringDataContext
                        {
                            Consumer = new RevSpringConsumer
                            {
                                ConsumerNumber = guarantorDetails.Id.ToString(),
                                FirstName = guarantorDetails?.Name?.FirstName ?? string.Empty,
                                LastName = guarantorDetails?.Name?.LastName ?? string.Empty,
                                Address1 = guarantorDetails.Address?.Street1 ?? string.Empty,
                                Address2 = guarantorDetails?.Address?.Street2 ?? string.Empty,
                                Country = guarantorDetails?.Address?.Country ?? string.Empty,
                                City = guarantorDetails?.Address?.City ?? string.Empty,
                                State = guarantorDetails?.Address?.State ?? string.Empty,
                                Zip = guarantorDetails?.Address?.ZipCode ?? string.Empty,
                                Phone = ExtractBasePhoneNumber(guarantorDetails.PhoneNumber),
                                Email = guarantorDetails?.Email ?? string.Empty,
                                DateOfBirth = guarantorDetails?.DateOfBirth.HasValue == true
                                              ? guarantorDetails.DateOfBirth.Value.ToString("dd-MM-yyyy")
                                              : string.Empty,
                                AmountDue = model.AmountDue
                            }

                        }
                    }
                };

                return Ok(payloadResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Controller}.{Action} - GetRevSpringPayload failed.", nameof(PaymentPostingController), nameof(GetRevSpringPayload));
                return BadRequest(ex.Message);
            }
        }

        private static string ExtractBasePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return string.Empty;
            }

            int idx = phoneNumber.IndexOfAny(new[] { 'x', 'X' });
            return idx >= 0 ? phoneNumber.Substring(0, idx).Trim() : phoneNumber.Trim();
        }
    }
}