using ClearingHouseService.Web.Interface;
using Microsoft.AspNetCore.Mvc;
using Rethink.Services.Common.Dtos.ClearingHouse;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace ClearingHouseService.Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClearingHouse270FileGeneratorController : ControllerBase
    {
        private readonly IClearingHouseProcessorFor270Edi _clearingHouseProcessor;     
        private readonly ILogger<ClearingHouse270FileGeneratorController> _logger;
        private readonly IBackgroundJobQueue _backgroundJobQueue;

        public ClearingHouse270FileGeneratorController(
            IClearingHouseProcessorFor270Edi clearingHouseProcessor,
            ILogger<ClearingHouse270FileGeneratorController> logger,
            IBackgroundJobQueue backgroundJobQueue)
        {
            _clearingHouseProcessor = clearingHouseProcessor;           
            _logger = logger;
            _backgroundJobQueue = backgroundJobQueue;
        }

        [HttpPost("upload270EdiData")]
        public async Task<IActionResult> Upload270EdiData([FromBody] Eligibility270Request eligibility270Request)
        {
            try
            {
                _logger.LogInformation("upload270EdiData called FunderId={FunderId},MemberId={MemberId},AccountId={AccountInfoId}",eligibility270Request.FunderId,eligibility270Request.MemberId,eligibility270Request.AccountInfoId);
                var (ediSuccess, edi270Result) = await _clearingHouseProcessor.Generate270EDIData(eligibility270Request);
                
                if (!ediSuccess || string.IsNullOrEmpty(edi270Result))
                {
                    _logger.LogInformation("ediResult is empty for FunderId={FunderId},MemberId={MemberId},AccountId={AccountInfoId}",eligibility270Request.FunderId,eligibility270Request.MemberId,eligibility270Request.AccountInfoId);
                    return BadRequest($"Failed to generate EDI for Funder Id: {eligibility270Request.FunderId}");
                }

                _logger.LogInformation($"Starting processing Funder with Id: {eligibility270Request.FunderId}");

                await _backgroundJobQueue.EnqueueAsync(
                    new StediEligibilityJobDTO
                    {
                        Edi270Request = edi270Result,
                        FunderId = eligibility270Request.FunderId,
                        MemberId = eligibility270Request.MemberId,
                        EffectiveDate = eligibility270Request.EffectiveDate,
                        AccountId= eligibility270Request.AccountInfoId
                    });

                return Ok($"270 EDI file successfully processed to Stedi Clearing House for Funder : {eligibility270Request.FunderName}");
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload270EdiDataToSftp Error processing edi");
                return StatusCode(500, "Internal server error" + ex.Message);
            }
        }
    }
}
