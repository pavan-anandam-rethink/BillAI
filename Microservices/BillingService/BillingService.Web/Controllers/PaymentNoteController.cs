using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class PaymentNoteController : Controller
    {
        private readonly IPaymentNoteService _noteService;
        private readonly ILogger<PaymentNoteController> _logger;

        public PaymentNoteController(IPaymentNoteService noteService, ILogger<PaymentNoteController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetAll([FromBody] int paymentId)
        {
            _logger.LogInformation("{Controller}.{Action} - GetAll called. PaymentId={PaymentId}",
                nameof(PaymentNoteController),
                nameof(GetAll),
                paymentId);

            try
            {
                var result = await _noteService.GetAll(paymentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentNoteController)}.{nameof(GetAll)} -GetAll failed. PaymentId={paymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] PaymentNoteSaveModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - Add called. PaymentId={PaymentId}",
                nameof(PaymentNoteController),
                nameof(Add),
                model?.PaymentId);

            try
            {
                var result = await _noteService.AddNote(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentNoteController)}.{nameof(Add)} -Add failed. PaymentId={model?.PaymentId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToSeveral([FromBody] PaymentNoteSaveModel[] model)
        {
            _logger.LogInformation("{Controller}.{Action} - AddToSeveral called. Count={Count}",
                nameof(PaymentNoteController),
                nameof(AddToSeveral),
                model?.Length);

            try
            {
                var result = await _noteService.AddToPaymentsAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentNoteController)}.{nameof(AddToSeveral)} -AddToSeveral failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] PaymentNoteDeleteModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - Delete called. NoteId={NoteId}",
                nameof(PaymentNoteController),
                nameof(Delete),
                model?.Id);

            try
            {
                var result = await _noteService.DeleteNote(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PaymentNoteController)}.{nameof(Delete)} -Delete failed. NoteId={model?.Id}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}