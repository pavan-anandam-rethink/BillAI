using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ChargeEntryController : Controller
    {
        private IChargeEntryService _chargeEntryService;
        private readonly IMapper _mapper;
        private readonly ILogger<ChargeEntryController> _logger;

        public ChargeEntryController(IMapper mapper, IChargeEntryService chargeEntryService, ILogger<ChargeEntryController> logger)
        {
            _mapper = mapper;
            _chargeEntryService = chargeEntryService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddNote([FromBody] AddNoteModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - AddNote called. ChargeId={ChargeId}",
                    nameof(ChargeEntryController),
                    nameof(AddNote),
                    model?.ChargeId);
            try
            {
                var result = await _chargeEntryService.AddChargeNoteAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChargeEntryController)}.{nameof(AddNote)} -AddNote failed. ChargeId={model?.ChargeId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNote([FromBody] int chargeId)
        {
            _logger.LogInformation("{Controller}.{Action} - DeleteNote called. ChargeId={   }",
                    nameof(ChargeEntryController),
                    nameof(DeleteNote),
                    chargeId);

            try
            {
                await _chargeEntryService.DeleteChargeNoteAsync(chargeId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ChargeEntryController)}.{nameof(DeleteNote)} - DeleteNote failed. ChargeId={chargeId}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

    }
}