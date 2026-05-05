using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class PatientInvoiceController : ControllerBase
    {
        public readonly IPatientInvoiceService _invoiceService;
        private readonly ILogger<PatientInvoiceController> _logger;
        public PatientInvoiceController(IPatientInvoiceService invoiceService, ILogger<PatientInvoiceController> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetPICreationDetails([FromBody] CreateInvoiceFilters filter)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPICreationDetails called. Filter={@Filter}",
                    nameof(PatientInvoiceController),
                    nameof(GetPICreationDetails),
                    filter);

            try
            {
                var (result, totalcount) = await _invoiceService.GetPICreationDetails(filter);
                return Ok(new { TotalCount = totalcount, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PatientInvoiceController)}.{nameof(GetPICreationDetails)} -GetPICreationDetails failed. Filter={filter}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetInvoiceDetails([FromBody] PendingCollectionFilters filter)
        {
            _logger.LogInformation("{Controller}.{Action} - GetInvoiceDetails called. Filter={@Filter}",
                    nameof(PatientInvoiceController),
                    nameof(GetInvoiceDetails),
                    filter);

            try
            {
                var (result, userList, totalcount) = await _invoiceService.GetInvoiceDetails(filter);
                return Ok(new { TotalCount = totalcount, Data = result, UserList = userList });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PatientInvoiceController)}.{nameof(GetInvoiceDetails)} -GetInvoiceDetails failed. Filter={filter}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PrintPreview([FromBody] List<InvoiceRequestModel> invoiceRequests)
        {
            _logger.LogInformation("{Controller}.{Action} - PrintPreview called. InvoiceCount={Count}",
                    nameof(PatientInvoiceController),
                    nameof(PrintPreview),
                    invoiceRequests?.Count);

            try
            {
                var (pdfData, errors) = await _invoiceService.GeneratePDF(invoiceRequests, false, false, null);
                var response = new PdfResponse
                {
                    PdfBase64 = pdfData != null ? Convert.ToBase64String(pdfData) : null,
                    Errors = errors
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PatientInvoiceController)}.{nameof(PrintPreview)} -PrintPreview failed. InvoiceCount={invoiceRequests?.Count}, ErrorMsg ={ex.Message}");
                return BadRequest(new { message = "An error occurred while processing the request.", details = ex.Message });
            }

        }

        [HttpPost]
        public async Task<IActionResult> PrintAndSubmit([FromBody] PrintAndSubmitRequestModel invoiceRequest)
        {
            _logger.LogInformation("{Controller}.{Action} - PrintAndSubmit called. IncludePreviousInvoices={IncludePrevious}",
                    nameof(PatientInvoiceController),
                    nameof(PrintAndSubmit),
                    invoiceRequest?.includePreviousInvoices);

            try
            {
                var (pdfData, errors) = await _invoiceService.GeneratePDF(invoiceRequest.InvoiceRequests, true, invoiceRequest.includePreviousInvoices, null);
                var response = new PdfResponse
                {
                    PdfBase64 = pdfData != null ? Convert.ToBase64String(pdfData) : null,
                    Errors = errors
                };
                return Ok(response);
            }
            catch (Exception ex)
            {

                _logger.LogError($"{nameof(PatientInvoiceController)}.{nameof(PrintAndSubmit)} -PrintAndSubmit failed. Request={invoiceRequest}, ErrorMsg ={ex.Message}");
                return BadRequest(new { message = "An error occurred while processing the request.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetInvoicePDF([FromBody] GetInvoicePDFRequestModel invoiceDetails)
        {
            _logger.LogInformation("{Controller}.{Action} - GetInvoicePDF called. AccountId={AccountId}, ClientId={ClientId}, InvoiceNo={InvoiceNo}",
                   nameof(PatientInvoiceController),
                   nameof(GetInvoicePDF),
                   invoiceDetails.AccountId,
                   invoiceDetails.ClientId,
                   invoiceDetails.InvoiceNo);

            try
            {
                var (pdfData, errors) = await _invoiceService.GetInvoicePDF(invoiceDetails.AccountId, invoiceDetails.ClientId, invoiceDetails.InvoiceNo);

                var response = new PdfResponse
                {
                    PdfBase64 = pdfData != null ? Convert.ToBase64String(pdfData) : null,
                    Errors = errors
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PatientInvoiceController)}.{nameof(GetInvoicePDF)} -GetInvoicePDF failed. AccountId={invoiceDetails.AccountId}, ClientId={invoiceDetails.ClientId}, InvoiceNo={invoiceDetails.InvoiceNo}, ErrorMsg ={ex.Message}");
                return BadRequest(new { message = "An error occurred while processing the request.", details = ex.Message });
            }
        }

    }
}
