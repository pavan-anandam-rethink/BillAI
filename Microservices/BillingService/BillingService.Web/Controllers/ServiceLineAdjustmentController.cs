using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Payment")]
    [Route("[controller]/[action]")]
    public class ServiceLineAdjustmentController : Controller
    {
        private readonly IPaymentServiceLineAdjustmentService _paymentServiceLineAdjustmentService;
        private readonly ILogger<ServiceLineAdjustmentController> _logger;

        public ServiceLineAdjustmentController(IPaymentServiceLineAdjustmentService paymentServiceLineAdjustmentService, ILogger<ServiceLineAdjustmentController> logger)
        {
            _paymentServiceLineAdjustmentService = paymentServiceLineAdjustmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetServiceLineAdjustments([FromBody] int serviceLineId)
        {
            _logger.LogInformation("{Controller}.{Action} - GetServiceLineAdjustments called. ServiceLineId={ServiceLineId}",
                    nameof(ServiceLineAdjustmentController),
                    nameof(GetServiceLineAdjustments),
                    serviceLineId);

            try
            {
                var result =
                    await _paymentServiceLineAdjustmentService.GetPaymentServiceLineAdjustments(serviceLineId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ServiceLineAdjustmentController)}.{nameof(GetServiceLineAdjustments)} -GetServiceLineAdjustments failed. ServiceLineId={serviceLineId}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetServiceLineAdjustmentsByCharge([FromBody] GetChargeDetailsModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetServiceLineAdjustmentsByCharge called. ChargeId={ChargeId}",
                    nameof(ServiceLineAdjustmentController),
                    nameof(GetServiceLineAdjustmentsByCharge),
                    model?.Id);

            try
            {
                var result =
                    await _paymentServiceLineAdjustmentService.GetPaymentServiceLineAdjustmentsByCharge(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ServiceLineAdjustmentController)}.{nameof(GetServiceLineAdjustmentsByCharge)} -GetServiceLineAdjustmentsByCharge failed. ChargeId={model?.Id}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddPaymentServiceLineAdjustments([FromBody] AddOrEditAdjustmentModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - AddPaymentServiceLineAdjustments called. ServiceLineId={ServiceLineId}",
                    nameof(ServiceLineAdjustmentController),
                    nameof(AddPaymentServiceLineAdjustments),
                    model?.ServiceLineId);

            try
            {
                var result =
                    await _paymentServiceLineAdjustmentService.AddPaymentServiceLineAdjustmentsAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ServiceLineAdjustmentController)}.{nameof(AddPaymentServiceLineAdjustments)} -AddPaymentServiceLineAdjustments failed. ServiceLineId={model?.ServiceLineId}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteServiceLineAdjustments([FromBody] IdsWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - DeleteServiceLineAdjustments called. IdCount={Count}",
                    nameof(ServiceLineAdjustmentController),
                    nameof(DeleteServiceLineAdjustments),
                    model?.Ids?.Count());

            try
            {
                await _paymentServiceLineAdjustmentService.DeleteServiceLineAdjustmentsAsync(model);

                return Ok();
            }
            catch (Exception ex)
            {
                var ids = model?.Ids != null ? string.Join(",", model.Ids) : string.Empty;
                _logger.LogError($"{nameof(ServiceLineAdjustmentController)}.{nameof(DeleteServiceLineAdjustments)} -DeleteServiceLineAdjustments failed. Ids={ids}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateServiceLineAdjustments(
            [FromBody] AddOrEditAdjustmentModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - UpdateServiceLineAdjustments called. ServiceLineId={ServiceLineId}",
                    nameof(ServiceLineAdjustmentController),
                    nameof(UpdateServiceLineAdjustments),
                    model?.ServiceLineId);

            try
            {
                var result =
                    await _paymentServiceLineAdjustmentService.UpdateServiceLineAdjustmentsAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ServiceLineAdjustmentController)}.{nameof(UpdateServiceLineAdjustments)} -UpdateServiceLineAdjustments failed. ServiceLineId={model?.ServiceLineId}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAdjustmentReasonDescriptions(
            [FromBody] string codes)
        {
            _logger.LogInformation("{Controller}.{Action} - GetAdjustmentReasonDescriptions called. Codes={Codes}",
                    nameof(ServiceLineAdjustmentController),
                    nameof(GetAdjustmentReasonDescriptions),
                    codes);

            try
            {
                var result =
                    await _paymentServiceLineAdjustmentService.GetAdjustmentReasonDescriptionsAsync(codes);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ServiceLineAdjustmentController)}.{nameof(GetAdjustmentReasonDescriptions)} -GetAdjustmentReasonDescriptions failed. Codes={codes}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReapplyPRAdjustmentsAfterSecondaryBilling(
            [FromBody] int claimId)
        {
            _logger.LogInformation("{Controller}.{Action} - ReapplyPRAdjustmentsAfterSecondaryBilling called. ClaimId={ClaimId}",
                    nameof(ServiceLineAdjustmentController),
                    nameof(ReapplyPRAdjustmentsAfterSecondaryBilling),
                    claimId);

            try
            {
                await _paymentServiceLineAdjustmentService
                    .ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ServiceLineAdjustmentController)}.{nameof(ReapplyPRAdjustmentsAfterSecondaryBilling)} -ReapplyPRAdjustmentsAfterSecondaryBilling failed. ClaimId={claimId}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

    }
}