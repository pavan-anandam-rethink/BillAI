using ClearingHouseService.Web.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClearingHouseService.Web.Controllers
{
    [Route("api")]
    [ApiController]
    public class ClearingHouseReferenceDataController : Controller
    {
        private readonly IClearingHouseReferenceDataProvider _service;

        public ClearingHouseReferenceDataController(IClearingHouseReferenceDataProvider service)
        {
            _service = service;
        }

        [HttpGet("Payers")]
        public async Task<IActionResult> GetPayersAsync(
            string clearinghouse,
            CancellationToken ct)
        {
            // Assuming _service.GetPayersAsync(ct) returns a string, convert it to a MemoryStream
            var payersData = await _service.GetPayersAsync(ct);
            var payersStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payersData));

            return new FileStreamResult(payersStream, "text/csv")
            {
                FileDownloadName = "stedi-payers.csv"
            };
        }

        [HttpGet("Enrollments")]
        public async Task<IActionResult> GetEnrollments(
            string clearinghouse,
            CancellationToken ct)
        {
            // Replace the invalid return statement with a proper NotImplementedException throw
            return new NotFoundResult();
        }
    }
}
