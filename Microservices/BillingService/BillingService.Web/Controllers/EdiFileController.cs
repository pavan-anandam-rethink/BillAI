using System;
using System.Threading.Tasks;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class EdiFileController : ControllerBase
    {
        private readonly IBillingFilePath _billingFilePath;
        private readonly ILogger<EdiFileController> _logger;

        public EdiFileController(IBillingFilePath billingFilePath, ILogger<EdiFileController> logger)
        {
            _billingFilePath = billingFilePath;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the EDI file content from Azure Blob Storage based on the provided search criteria.
        /// </summary>
        /// <param name="model">The model containing filter criteria such as AccountInfoId, ClaimSubmissionId, ClaimId, PaymentId, and FileType.</param>
        /// <returns>The EDI file content as a string, or an empty string if no matching record or blob is found.</returns>
        [HttpPost]
        public async Task<IActionResult> GetEdiFilesFromBlob([FromBody] ClaimEdiFilesModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetEdiFilesFromBlob called. ClaimId={ClaimId}",
                nameof(EdiFileController), nameof(GetEdiFilesFromBlob), model.ClaimId);

            try
            {
                var result = await _billingFilePath.GetEdiFilesFromBlob(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Controller}.{Action} - GetEdiFilesFromBlob failed. ClaimId={ClaimId}, ErrorMsg={ErrorMsg}",
                    nameof(EdiFileController), nameof(GetEdiFilesFromBlob), model.ClaimId, ex.Message);

                return BadRequest(ex.Message);
            }
        }
    }
}
