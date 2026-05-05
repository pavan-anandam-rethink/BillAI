using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ClaimNoteController : Controller
    {
        private readonly IClaimNoteService _noteService;
        private readonly ILogger<ClaimNoteController> _logger;

        public ClaimNoteController(IClaimNoteService noteService, ILogger<ClaimNoteController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetAll([FromBody] ClaimNoteGetAllModel model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - GetAll called. ClaimId={ClaimId}",
                    nameof(ClaimNoteController),
                    nameof(GetAll),
                    model?.Id);

                var result = await _noteService.GetAllAsync(model);

                return Ok(result);
            }
            catch (SqlException ex)
            {
                _logger.LogError($"{nameof(ClaimNoteController)}.{nameof(GetAll)} -GetAll failed. ClaimId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] ClaimNoteSaveModel model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Add called. ClaimId={ClaimId}, MemberId={MemberId}",
                    nameof(ClaimNoteController),
                    nameof(Add),
                    model.ClaimId,
                    model.MemberId);

                var result = await _noteService.AddAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimNoteController)}.{nameof(Add)} -Add failed. ClaimId={model.ClaimId}, MemberId={model.MemberId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToSeveral([FromBody] ClaimNoteRequestModel model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - AddToSeveral called. MemberId={MemberId}",
                    nameof(ClaimNoteController),
                    nameof(AddToSeveral),
                    model?.MemberId);

                var result = await _noteService.AddToClaimsAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimNoteController)}.{nameof(AddToSeveral)} -AddToSeveral failed. MemberId={model?.MemberId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] ClaimNoteDeleteModel model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Delete called. ClaimId={Id}",
                    nameof(ClaimNoteController),
                    nameof(Delete),
                    model.Id);

                var result = await _noteService.DeleteAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimNoteController)}.{nameof(Delete)} -Delete failed. ClaimId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

    }
}