using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ClaimPostingController : Controller
    {
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IChargePaymentService _chargePaymentService;
        private readonly IReportService _reportService;
        private readonly ILogger<ClaimPostingController> _logger;

        public ClaimPostingController(
            IPaymentClaimService paymentClaimService,
            IChargePaymentService chargePaymentService,
            IReportService reportService,
            ILogger<ClaimPostingController> logger)
        {
            _paymentClaimService = paymentClaimService;
            _chargePaymentService = chargePaymentService;
            _reportService = reportService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetClaims([FromBody] GetClaimFilterModel getClaimsModel)
        {
            _logger.LogInformation("{Controller}.{Action} - GetClaims called. AccountInfoId={AccountInfoId}, PaymentId={PaymentId}, MemberId={MemberId}",
                nameof(ClaimPostingController),
                nameof(GetClaims),
                getClaimsModel?.AccountInfoId,
                getClaimsModel?.PaymentId,
                getClaimsModel?.MemberId);

            try
            {
                var result = await _paymentClaimService.GetPaymentClaimsAsync(getClaimsModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimPostingController)}.{nameof(GetClaims)} -GetClaims failed. AccountInfoId={getClaimsModel?.AccountInfoId}, PaymentId={getClaimsModel?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentPatients(int paymentId)
        {
            try
            {
                _logger.LogInformation("Getting payment patients. PaymentId={PaymentId}", paymentId);

                var result = await _paymentClaimService.GetPatientsByPaymentAsync(paymentId);

                _logger.LogInformation("Successfully got payment patients. PaymentId={PaymentId}", paymentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get payment patients. PaymentId={paymentId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPatientClaims([FromBody] GetClaimsModel getClaimsModel)
        {
            try
            {
                _logger.LogInformation("Getting patient claims. AccountInfoId={AccountInfoId}, PaymentId={PaymentId}",
                    getClaimsModel?.AccountInfoId, getClaimsModel?.PaymentId);

                var result = await _paymentClaimService.GetPaymentClaimsByPatientsAsyncNew(getClaimsModel);

                _logger.LogInformation("Successfully got patient claims. AccountInfoId={AccountInfoId}, PaymentId={PaymentId}",
                    getClaimsModel?.AccountInfoId, getClaimsModel?.PaymentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get patient claims. AccountInfoId={getClaimsModel?.AccountInfoId}, PaymentId={getClaimsModel?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEOBClaims([FromBody] int paymentId)
        {
            try
            {
                _logger.LogInformation("Getting EOB claims. PaymentId={PaymentId}", paymentId);

                var result = await _paymentClaimService.GetEOBClaimsAsync(paymentId, null);

                _logger.LogInformation("Successfully got EOB claims. PaymentId={PaymentId}", paymentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get EOB claims. PaymentId={paymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEOBPaymentClaimsPDF([FromBody] GetEOBClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Getting EOB PDF. PaymentId={PaymentId}, ClaimsCount={ClaimsCount}",
                    model?.PaymentId, model?.Claims?.Count);

                var result = await _paymentClaimService.GetEOBPaymentClaimPDFAsync(model);

                _logger.LogInformation("Successfully got EOB PDF. PaymentId={PaymentId}", model?.PaymentId);

                return File(result,
                    MediaTypeNames.Application.Pdf,
                    "EOB Details");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get EOB PDF. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSelectedEOBClaims([FromBody] GetEOBClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Getting selected EOB claims. PaymentId={PaymentId}, ClaimsCount={ClaimsCount}",
                    model?.PaymentId, model?.Claims?.Count);

                var result = await _paymentClaimService.GetEOBClaimsAsync(model.PaymentId, model.Claims);

                _logger.LogInformation("Successfully got selected EOB claims. PaymentId={PaymentId}", model?.PaymentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get selected EOB claims. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimDetails([FromBody] IdWithUserInfo model)
        {
            try
            {
                _logger.LogInformation("Getting claim details. AccountInfoId={AccountInfoId}, MemberId={MemberId}, ClaimId={ClaimId}",
                    model?.AccountInfoId, model?.MemberId, model?.Id);

                var result = await _paymentClaimService.GetPaymentClaimAsync(model.Id);

                _logger.LogInformation("Successfully got claim details. ClaimId={ClaimId}", model?.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get claim details. ClaimId={model?.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPatientDetails([FromBody] PatientDetailsModel model)
        {
            try
            {
                _logger.LogInformation("Getting patient details. AccountInfoId={AccountInfoId}, PatientId={PatientId}, MemberId={MemberId}",
                    model?.AccountInfoId, model?.patientId, model?.MemberId);

                var result = await _paymentClaimService.getPatientDetails(model.patientId, model.AccountInfoId);

                _logger.LogInformation("Successfully got patient details. PatientId={PatientId}", model?.patientId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get patient details. PatientId={model?.patientId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentClaimServiceLines([FromBody] int claimId)
        {
            try
            {
                _logger.LogInformation("Getting payment claim service lines. ClaimId={ClaimId}", claimId);

                var result = await _paymentClaimService.GetPaymentClaimServiceLinesAsync(claimId);

                _logger.LogInformation("Successfully got payment claim service lines. ClaimId={ClaimId}, Count={Count}", claimId, result?.Count);

                return Ok(new { data = result, totalCount = result.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get payment claim service lines. ClaimId={claimId}, ErrorMsg ={ex.Message} ");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPatientPaymentClaimLinkedServiceLines([FromBody] GetPatientPaymentServiceLinesModel model)
        {
            try
            {
                _logger.LogInformation("Getting linked patient payment service lines. PaymentId={PaymentId}, PatientId={PatientId}",
                    model?.PaymentId, model?.PatientId);

                var result = await _paymentClaimService.GetPatientPaymentLinkedServiceLinesAsyncNew(model);

                _logger.LogInformation("Successfully got linked patient payment service lines. PaymentId={PaymentId}, PatientId={PatientId}, Count={Count}",
                    model?.PaymentId, model?.PatientId, result?.Count);

                return Ok(new { data = result, totalCount = result.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get linked patient payment service lines. PaymentId={model?.PaymentId}, PatientId={model?.PatientId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPatientPaymentClaimUnlinkedServiceLines([FromBody] GetPatientPaymentServiceLinesModel model)
        {
            try
            {
                _logger.LogInformation("Getting unlinked patient payment service lines. PaymentId={PaymentId}, PatientId={PatientId}",
                    model?.PaymentId, model?.PatientId);

                var result = await _paymentClaimService.GetPatientPaymentUnlinkedServiceLinesAsyncNew(model);

                _logger.LogInformation("Successfully got unlinked patient payment service lines. PaymentId={PaymentId}, PatientId={PatientId}, Count={Count}",
                    model?.PaymentId, model?.PatientId, result?.Count);

                return Ok(new { data = result, totalCount = result.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get unlinked patient payment service lines. PaymentId={model?.PaymentId}, PatientId={model?.PatientId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentClaimServiceLine([FromBody] int serviceLineId)
        {
            try
            {
                _logger.LogInformation("Getting payment claim service line. ServiceLineId={ServiceLineId}", serviceLineId);

                var result = await _paymentClaimService.GetPaymentClaimServiceLineAsync(serviceLineId);

                _logger.LogInformation("Successfully got payment claim service line. ServiceLineId={ServiceLineId}", serviceLineId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get payment claim service line. ServiceLineId={serviceLineId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentPatientClaims([FromBody] CreatePatientClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Creating payment patient claims. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.PaymentId, model?.AccountInfoId, model?.MemberId);

                var result = await _paymentClaimService.CreatePaymentClaimsAsync(model);

                _logger.LogInformation("Successfully created payment patient claims. PaymentId={PaymentId}", model?.PaymentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create payment patient claims. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateClaimsToEraPayment([FromBody] CreateEraClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Creating claims to ERA payment. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.PaymentId, model?.AccountInfoId, model?.MemberId);

                var result = await _paymentClaimService.CreateClaimsToEraAsync(model);

                _logger.LogInformation("Successfully created claims to ERA payment. PaymentId={PaymentId}", model?.PaymentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create claims to ERA payment. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePaymentClaimServiceLineAmounts([FromBody] UpdatePaymentServiceLineAmountsModelWithUserInfo modelWithUserInfo)
        {
            try
            {
                _logger.LogInformation("Updating payment claim service line amounts. ServiceLineId={ServiceLineId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    modelWithUserInfo?.ServiceLineId, modelWithUserInfo?.AccountInfoId, modelWithUserInfo?.MemberId);

                await _paymentClaimService.UpdatePaymentClaimServiceLineAmountsAsync(modelWithUserInfo);

                _logger.LogInformation("Successfully updated payment claim service line amounts. ServiceLineId={ServiceLineId}", modelWithUserInfo?.ServiceLineId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update payment claim service line amounts. ServiceLineId={modelWithUserInfo?.ServiceLineId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostManualPaymentClaimLines([FromBody] PostPaymentClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Posting manual payment claim lines. PaymentId={PaymentId}", model?.PaymentId);

                await _paymentClaimService.PostPaymentClaimLines(model);

                _logger.LogInformation("Successfully posted manual payment claim lines. PaymentId={PaymentId}", model?.PaymentId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to post manual payment claim lines. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostManualPatientPaymentClaimLines([FromBody] PostRemovePatientClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Posting manual patient payment claim lines. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.PaymentId, model?.AccountInfoId, model?.MemberId);

                var result = await _paymentClaimService.PostPatientPaymentClaimLinesAsync(model);

                _logger.LogInformation("Successfully posted manual patient payment claim lines. PaymentId={PaymentId}", model?.PaymentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to post manual patient payment claim lines. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentClaimErrors([FromBody] GetByIdSortFilterWithUserInfo model)
        {
            try
            {
                _logger.LogInformation("Getting payment claim errors. Id={Id}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.Id, model?.AccountInfoId, model?.MemberId);

                var result = await _paymentClaimService.GetPaymentClaimErrorsAsync(model);

                _logger.LogInformation("Successfully got payment claim errors. Id={Id}", model?.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get payment claim errors. Id={model?.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemovePaymentClaims([FromBody] RemovePaymentClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Removing payment claims. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.PaymentId, model?.AccountInfoId, model?.MemberId);

                await _paymentClaimService.RemoveSelectedClaimsAsync(model);

                _logger.LogInformation("Successfully removed payment claims. PaymentId={PaymentId}", model?.PaymentId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to remove payment claims. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemovePatientPaymentClaims([FromBody] PostRemovePatientClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Removing patient payment claims. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.PaymentId, model?.AccountInfoId, model?.MemberId);

                await _paymentClaimService.RemoveSelectedPatientClaimsAsync(model);

                _logger.LogInformation("Successfully removed patient payment claims. PaymentId={PaymentId}", model?.PaymentId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to remove patient payment claims. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSelectedPatientPaymentAmounts([FromBody] PostRemovePatientClaimsModel model)
        {
            try
            {
                _logger.LogInformation("Removing selected patient payment amounts. PaymentId={PaymentId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.PaymentId, model?.AccountInfoId, model?.MemberId);

                await _paymentClaimService.RemoveSelectedPatientPaymentAmountsAsync(model);

                _logger.LogInformation("Successfully removed selected patient payment amounts. PaymentId={PaymentId}", model?.PaymentId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to remove selected patient payment amounts. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentClaimServiceLinesSmall([FromBody] GetChargeDetailsModel model)
        {
            try
            {
                _logger.LogInformation("Getting payment claim service lines small. Id={Id}, IsServiceLine={IsServiceLine}", model?.Id, model?.IsServiceLine);

                var result = await _paymentClaimService.GetPaymentClaimServiceLinesSmallAsync(model);

                _logger.LogInformation("Successfully got payment claim service lines small. Id={Id}", model?.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get payment claim service lines small. Id={model?.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClientPrintDataById([FromBody] GetClientPrintDataRequest model)
        {
            try
            {
                _logger.LogInformation("Getting client print data. PatientId={PatientId}, ClaimId={ClaimId}, AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                    model?.PatientId, model?.ClaimId, model?.AccountInfoId, model?.MemberId);

                var paymentInfo = await _paymentClaimService.GetCompanyAccountInfoByPatientId(model);

                _logger.LogInformation("Successfully got client print data. PatientId={PatientId}, ClaimId={ClaimId}", model?.PatientId, model?.ClaimId);

                return Json(paymentInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get client print data. PatientId={model?.PatientId}, ClaimId={model?.ClaimId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendReport([FromBody] ReportQueryModel reportQueryModel = null)
        {
            try
            {
                if (reportQueryModel == null)
                    return BadRequest();

                _logger.LogInformation("Sending report. ReportFrequency={ReportFrequency}", reportQueryModel.ReportFrequency);

                return reportQueryModel.ReportFrequency switch
                {
                    ReportFrequency.Monthly => Ok(await _reportService.SendMonthlyReportAsync(reportQueryModel)),
                    ReportFrequency.Weekly => Ok(await _reportService.SendWeeklyReportAsync(reportQueryModel)),
                    _ => BadRequest()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending report. ReportFrequency={reportQueryModel?.ReportFrequency}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.InnerException?.Message ?? ex.Message.ToString());
            }
        }
    }
}