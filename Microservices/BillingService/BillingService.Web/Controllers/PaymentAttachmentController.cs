using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class PaymentAttachmentController : Controller
    {
        private readonly IPaymentAttachmentService _paymentAttachmentService;
        private readonly ILogger<PaymentAttachmentController> _logger;

        public PaymentAttachmentController(IPaymentAttachmentService paymentAttachmentService, ILogger<PaymentAttachmentController> logger)
        {
            _paymentAttachmentService = paymentAttachmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromBody] PaymentUploadModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - UploadFile called. PaymentId={PaymentId}",
                nameof(PaymentAttachmentController),
                nameof(UploadFile),
                model.PaymentId);

            try
            {
                var result = await _paymentAttachmentService.UploadFile(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentAttachmentController)}.{nameof(UploadFile)} -UploadFile failed. PaymentId={model.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUpload([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - DeleteUpload called. AttachmentId={AttachmentId}",
                nameof(PaymentAttachmentController),
                nameof(DeleteUpload),
                model.Id);

            try
            {
                await _paymentAttachmentService.DeleteUpload(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentAttachmentController)}.{nameof(DeleteUpload)} -DeleteUpload failed. AttachmentId={model.Id}, ErrorMsg= {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUploads([FromBody] DeleteAttachmentsModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - DeleteUploads called.",
                nameof(PaymentAttachmentController),
                nameof(DeleteUploads));

            try
            {
                await _paymentAttachmentService.DeleteUploads(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentAttachmentController)}.{nameof(DeleteUploads)} -DeleteUploads failed. ErrorMsg= {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetFileUpload([FromBody] int id)
        {
            _logger.LogInformation("{Controller}.{Action} - GetFileUpload called. AttachmentId={AttachmentId}",
                nameof(PaymentAttachmentController),
                nameof(GetFileUpload),
                id);

            try
            {
                var attachmentReturnModel = await _paymentAttachmentService.GetUpload(id);
                return File(
                    attachmentReturnModel.MemoryStream,
                    MediaTypeNames.Application.Octet,
                    attachmentReturnModel.Filename);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentAttachmentController)}.{nameof(GetFileUpload)} -GetFileUpload failed. AttachmentId={id}. ErrorMsg= {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentAttachments([FromBody] GetByIdSortFilterWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPaymentAttachments called. PaymentId={PaymentId}",
                nameof(PaymentAttachmentController),
                nameof(GetPaymentAttachments),
                model.Id);

            try
            {
                var result = await _paymentAttachmentService.GetPaymentAttachmentsAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentAttachmentController)}.{nameof(GetPaymentAttachments)} -GetPaymentAttachments failed. PaymentId={model.Id}. ErrorMsg= {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RenameAttachment([FromBody] RenameAttachmentModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - RenameAttachment called. AttachmentId={AttachmentId}",
                nameof(PaymentAttachmentController),
                nameof(RenameAttachment),
                model.AttachmentId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                            .Where(x => x.Value.Errors.Count > 0)
                            .SelectMany(x => x.Value.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();

                var errorMessage = string.Join(" | ", errors);

                _logger.LogError($"{nameof(PaymentAttachmentController)}.{nameof(RenameAttachment)} failed. " + $"AttachmentId={model?.AttachmentId}, Errors={errorMessage}");
                return BadRequest(errorMessage);
            }

            try
            {
                await _paymentAttachmentService.RenameAttachmentAsync(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentAttachmentController)}.{nameof(RenameAttachment)} -RenameAttachment failed. AttachmentId={model.AttachmentId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}