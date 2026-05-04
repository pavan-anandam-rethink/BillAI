using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ChargePaymentController : Controller
    {
        private readonly IChargePaymentService _chargePaymentService;
        private readonly IMapper _mapper;
        private readonly ILogger<ChargePaymentController> _logger;

        public ChargePaymentController(IMapper mapper, IChargePaymentService chargePaymentService,
        ILogger<ChargePaymentController> logger)
        {
            _mapper = mapper;
            _chargePaymentService = chargePaymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetForClaim(ClaimIdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetForClaim called. ClaimId={ClaimId}, AccountInfoId={AccountInfoId}",
                    nameof(ChargePaymentController),
                    nameof(GetForClaim),
                    model?.Id, model?.AccountInfoId);
            try
            {
                var chargeEntries =
                    _mapper.Map<List<ChargePaymentModel>>(
                        await _chargePaymentService.GetForClaim(model.Id, model.AccountInfoId));
                return Json(chargeEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChargePaymentController)}.{nameof(GetForClaim)} -GetForClaim failed. ClaimId={model?.Id}, AccountInfoId={model?.AccountInfoId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentOptions(ClaimIdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPaymentOptions called. ClaimId={ClaimId}, AccountInfoId={AccountInfoId}",
                    nameof(ChargePaymentController),
                    nameof(GetPaymentOptions),
                    model?.Id, model?.AccountInfoId);
            try
            {
                var paymentOptions =
                await _chargePaymentService.GetPaymentOptions(model.Id, model.AccountInfoId);
                return Json(paymentOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChargePaymentController)}.{nameof(GetPaymentOptions)} -GetPaymentOptions failed. ClaimId={model?.Id}, AccountInfoId={model?.AccountInfoId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetRemainingAmount(ChargeIdWithUserInfo chargeIdWithUserInfo)
        {
            _logger.LogInformation("{Controller}.{Action} - GetRemainingAmount called. ChargeId={ChargeId}, AccountInfoId={AccountInfoId}",
                    nameof(ChargePaymentController),
                    nameof(GetRemainingAmount),
                    chargeIdWithUserInfo?.ChargeId, chargeIdWithUserInfo?.AccountInfoId);
            try
            {
                var remainingAmount =
                    await _chargePaymentService.GetRemainingAmount(chargeIdWithUserInfo.ChargeId, chargeIdWithUserInfo.AccountInfoId);
                return Json(remainingAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChargePaymentController)}.{nameof(GetRemainingAmount)} -GetRemainingAmount failed. ChargeId={chargeIdWithUserInfo?.ChargeId}, AccountInfoId={chargeIdWithUserInfo?.AccountInfoId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save(ChargePaymentModelWithUserInfo paymentModelWithUserInfo)
        {
            _logger.LogInformation("{Controller}.{Action} - Save ChargePayment called. MemberId={MemberId}",
                    nameof(ChargePaymentController),
                    nameof(Save),
                    paymentModelWithUserInfo?.MemberId);
            try
            {
                var item = _mapper.Map<ChargePaymentItem>(paymentModelWithUserInfo.ChargePaymentModel);
                var updatedChargePayment = await _chargePaymentService.Save(item, paymentModelWithUserInfo.MemberId);
                return Json(updatedChargePayment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChargePaymentController)}.{nameof(Save)} -Save ChargePayment failed. MemberId={paymentModelWithUserInfo?.MemberId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(ChargePaymentModelWithUserInfo chargePaymentModelWithUserInfo)
        {
            _logger.LogInformation("{Controller}.{Action} - Delete ChargePayment called. MemberId={MemberId}",
                    nameof(ChargePaymentController),
                    nameof(Delete),
                    chargePaymentModelWithUserInfo?.MemberId);
            try
            {
                var item = _mapper.Map<ChargePaymentItem>(chargePaymentModelWithUserInfo.ChargePaymentModel);
                item = await _chargePaymentService.Delete(item, chargePaymentModelWithUserInfo.MemberId);
                return Json(item);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChargePaymentController)}.{nameof(Delete)} -Delete ChargePayment failed. MemberId={chargePaymentModelWithUserInfo?.MemberId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}