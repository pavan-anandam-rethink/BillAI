using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.BulkPaymentPosting;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Payment")]
    [Route("[controller]/[action]")]
    public class BulkPaymentPostingController : Controller
    {
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IBulkPaymentPostingService _bulkPaymentPostingService;
        private readonly IFunderService _funderService;
        private readonly IPaymentServiceLineAdjustmentService _paymentServiceLineAdjustmentService;
        private readonly IMapper _mapper;
        private readonly ILogger<AppointmentController> _logger;


        public BulkPaymentPostingController(IPaymentClaimService paymentClaimService,
            IBulkPaymentPostingService bulkPaymentPostingService,
            IFunderService funderService,
            IPaymentServiceLineAdjustmentService paymentServiceLineAdjustmentService,
            IMapper mapper,
            ILogger<AppointmentController> logger)
        {
            _paymentClaimService = paymentClaimService;
            _bulkPaymentPostingService = bulkPaymentPostingService;
            _funderService = funderService;
            _paymentServiceLineAdjustmentService = paymentServiceLineAdjustmentService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<List<BulkPaymentResponseModel>>> GetAllPaymentsForPosting([FromBody] BulkPaymentPostingRequestModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetAllPaymentsForPosting called. AccountInfoId={AccountInfoId}",
                                    nameof(BulkPaymentPostingController),
                                    nameof(GetAllPaymentsForPosting),
                                    model?.AccountInfoId);
            try
            {
                var result = await _bulkPaymentPostingService.GetAllPayments(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(BulkPaymentPostingController)}.{nameof(GetAllPaymentsForPosting)} -GetAllPaymentsForPosting failed. AccountInfoId={model?.AccountInfoId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdateBulkPaymentPostingAdjustments([FromBody] List<AddOrEditAdjustmentModelForBulkPosting> model)
        {
            _logger.LogInformation("{Controller}.{Action} - AddOrUpdateBulkPaymentPostingAdjustments called. ItemsCount={ItemsCount}",
                                    nameof(BulkPaymentPostingController),
                                    nameof(AddOrUpdateBulkPaymentPostingAdjustments),
                                    model?.Count);

            try
            {
                var chargeIds = new List<int>();
                chargeIds.AddRange(await DeleteBulkPaymentServiceLineAmounts(model));
                foreach (var data in model)
                {
                    try
                    {
                        var result = await _paymentServiceLineAdjustmentService.AddPaymentServiceLineAdjustmentsAsync(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error adding or updating bulk payment posting adjustments for ServiceLineId: {data.ServiceLineId}, AccountInfoId: {data.AccountInfoId}, MemberId: {data.MemberId}");
                        chargeIds.Add(data.ServiceLineId);
                    }
                }
                chargeIds.AddRange(await UpdateBulkPaymentServiceLineAmounts(model));

                return Ok(chargeIds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(BulkPaymentPostingController)}.{nameof(AddOrUpdateBulkPaymentPostingAdjustments)} -AddOrUpdateBulkPaymentPostingAdjustments failed. ItemsCount={model?.Count}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        private async Task<List<int>> UpdateBulkPaymentServiceLineAmounts(List<AddOrEditAdjustmentModelForBulkPosting> model)
        {
            var chargeIds = new List<int>();
            foreach (var data in model)
            {
                try
                {
                    _logger.LogInformation($"Updating bulk payment service line amounts for ServiceLineId: {data.ServiceLineId}, AccountInfoId: {data.AccountInfoId}, MemberId: {data.MemberId} with AllowedAmount: {data.AllowedAmount}, PaymentAmount: {data.PaymentAmount}");
                    var updatePaymentData = new UpdatePaymentServiceLineAmountsModelWithUserInfo
                    {
                        AccountInfoId = data.AccountInfoId,
                        MemberId = data.MemberId,
                        ServiceLineId = data.ServiceLineId,
                        AllowedAmount = data.AllowedAmount,
                        PaymentAmount = data.PaymentAmount
                    };
                    await _paymentClaimService.UpdatePaymentClaimServiceLineAmountsAsync(updatePaymentData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating bulk payment service line amounts for ServiceLineId: {data.ServiceLineId}, AccountInfoId: {data.AccountInfoId}, MemberId: {data.MemberId}");
                    chargeIds.Add(data.ServiceLineId);
                }
            }
            return chargeIds;
        }

        private async Task<List<int>> DeleteBulkPaymentServiceLineAmounts(List<AddOrEditAdjustmentModelForBulkPosting> model)
        {
            var chargeIds = new List<int>();
            foreach (var data in model)
            {
                try
                {
                    _logger.LogInformation($"Deleting bulk payment service line amounts for ServiceLineId: {data.ServiceLineId}, AccountInfoId: {data.AccountInfoId}, MemberId: {data.MemberId}");
                    await _paymentServiceLineAdjustmentService.DeleteServiceLineAdjustmentsAsync(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting bulk payment service line amounts for ServiceLineId: {data.ServiceLineId}, AccountInfoId: {data.AccountInfoId}, MemberId: {data.MemberId}");
                    chargeIds.Add(data.ServiceLineId);
                }
            }
            return chargeIds;
        }
    }
}