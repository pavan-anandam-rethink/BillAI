using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Services.Billing;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Models.ReportingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class AppointmentReportsController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly IAppointmentService _appointmentService;
        private readonly IAppointmentReportService _appointmentReportService;
        private readonly IClaimSearchService _claimSearchService;
        private readonly ILogger<AppointmentReportsController> _logger;

        public AppointmentReportsController(IBaseHttpClient httpClient,
            IConfiguration configuration,
            IAppointmentService appointmentService,
            IAppointmentReportService appointmentReportService,
            IClaimSearchService claimSearchService,
            ILogger<AppointmentReportsController> logger)
        : base(httpClient, configuration)
        {
            _configuration = configuration;
            _appointmentService = appointmentService;
            _appointmentReportService = appointmentReportService;
            _claimSearchService = claimSearchService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetUnbilledAppointments([FromBody] UnbilledAppointmentsRequestModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetUnbilledAppointments called. AccountInfoId={AccountInfoId}, MemberId={MemberId}, StartDate={StartDate}, EndDate={EndDate}",
                nameof(AppointmentReportsController),
                nameof(GetUnbilledAppointments),
                model?.AccountInfoId, model?.MemberId, model?.StartDate, model?.EndDate);

            try
            {
                var result = await _appointmentReportService.GetUnbilledAppointmentDetails(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetUnbilledAppointments)} - Failed to get unbilled appointments. AccountInfoId={model?.AccountInfoId}, MemberId={model?.MemberId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = "Your request is invalid. Please check the input and try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateClaimsForUnbilledAppointments([FromBody] IdsWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - CreateClaimsForUnbilledAppointments called. AccountInfoId={AccountInfoId}, MemberId={MemberId}, IdCount={Count}",
                nameof(AppointmentReportsController),
                nameof(CreateClaimsForUnbilledAppointments),
                model?.AccountInfoId, model?.MemberId, model?.Ids?.Length ?? 0);

            try
            {
                var result = await _appointmentReportService.CreateClaimsForUnbilledAppointmentsAsync(model.AccountInfoId, model.MemberId, model.Ids);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(CreateClaimsForUnbilledAppointments)} - Failed to create claims for unbilled appointments. AccountInfoId={model?.AccountInfoId}, MemberId={model?.MemberId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportUnbilledAppointmentData([FromBody] UnbilledAppointmentsRequestModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - ExportUnbilledAppointmentData called. AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                nameof(AppointmentReportsController),
                nameof(ExportUnbilledAppointmentData),
                model?.AccountInfoId, model?.MemberId);

            try
            {
                var filterForExport = await GetFiltersForExport(model);

                var exportModel = new ExportModelForUnbilledAppointments
                {
                    Model = model,
                    Filter = filterForExport
                };

                var result = await _appointmentReportService.ExportUnbilledAppointmentDataAsync(exportModel);
                var base64Excel = Convert.ToBase64String(result);
                return Ok(new { data = base64Excel });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(ExportUnbilledAppointmentData)} - Failed to export unbilled appointment data. AccountInfoId={model?.AccountInfoId}, MemberId={model?.MemberId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = "Failed to export data. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClientListByIds([FromBody] UserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetClientListByIds called. AccountInfoId={AccountInfoId}",
                nameof(AppointmentReportsController),
                nameof(GetClientListByIds),
                model?.AccountInfoId);

            try
            {
                var result = await _claimSearchService.GetAllClientsForAccount(model.AccountInfoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetClientListByIds)} - Failed to get client list. AccountInfoId={model?.AccountInfoId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetFunderListByIds([FromBody] UserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetFunderListByIds called. AccountInfoId={AccountInfoId}",
                nameof(AppointmentReportsController),
                nameof(GetFunderListByIds),
                model?.AccountInfoId);

            try
            {
                var result = await _claimSearchService.GetFunderInfoByIds(model.AccountInfoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetFunderListByIds)} - Failed to get funder list. AccountInfoId={model?.AccountInfoId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClientHistoryFunderListByIds([FromBody] ClientHistoryUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetClientHistoryFunderListByIds called. AccountInfoId={AccountInfoId}, ClientId={ClientId}",
                nameof(AppointmentReportsController),
                nameof(GetClientHistoryFunderListByIds),
                model?.AccountInfoId, model?.ClientId);

            try
            {
                var result = await _claimSearchService.GetClientHistoryFunderInfoByIds(model.AccountInfoId, model.ClientId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetClientHistoryFunderListByIds)} - Failed to get client history funder list. AccountInfoId={model?.AccountInfoId}, ClientId={model?.ClientId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetStaffListByIds([FromBody] UserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetStaffListByIds called. AccountInfoId={AccountInfoId}",
                nameof(AppointmentReportsController),
                nameof(GetStaffListByIds),
                model?.AccountInfoId);

            try
            {
                var result = await _claimSearchService.GetStaffInfoByIds(model.AccountInfoId);
                return Ok(result.Where(x => !string.IsNullOrEmpty(x.Name)));
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetStaffListByIds)} - Failed to get staff list. AccountInfoId={model?.AccountInfoId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPoSListByIds([FromBody] UserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetPoSListByIds called. AccountInfoId={AccountInfoId}",
                nameof(AppointmentReportsController),
                nameof(GetPoSListByIds),
                model?.AccountInfoId);

            try
            {
                var result = await _claimSearchService.GetPlaceOfServiceInfoByIds(model.AccountInfoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetPoSListByIds)} - Failed to get place of service list. AccountInfoId={model?.AccountInfoId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetLocationListByIds([FromBody] UserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetLocationListByIds called. AccountInfoId={AccountInfoId}",
                nameof(AppointmentReportsController),
                nameof(GetLocationListByIds),
                model?.AccountInfoId);

            try
            {
                var result = await _claimSearchService.GetLocationInfoByIds(model.AccountInfoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetLocationListByIds)} - Failed to get location list. AccountInfoId={model?.AccountInfoId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<UnbilledAppointmentsRequestModelForExport> GetFiltersForExport(BaseAppointmentsRequestModel model)
        {
            var data = await GetFilterDataAsync(model.AccountInfoId);

            return new UnbilledAppointmentsRequestModelForExport
            {
                AccountInfoId = model.AccountInfoId,
                MemberId = model.MemberId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Clients = ApplyFilter(data.clients, model.Clients, x => x.Id, x => x.Name),
                PayerOrFunder = ApplyFilter(data.funders, model.PayerOrFunder, x => x.Id, x => x.Name),
                Staff = ApplyFilter(data.staff, model.Staff, x => x.TypeId, x => x.Name),
                PlaceOfService = ApplyFilter(data.pos, model.PlaceOfService, x => x.Id, x => x.Name),
                Location = ApplyFilter(data.locations, model.Location, x => x.Id, x => x.Name)
            };
        }

        private async Task<(
            IEnumerable<BaseNameOption> clients,
            IEnumerable<BaseNameOption> funders,
            IEnumerable<StaffBaseNameOption> staff,
            IEnumerable<BaseNameOption> pos,
            IEnumerable<BaseNameOption> locations)>
        GetFilterDataAsync(int accountId)
        {
            var clients = await _claimSearchService.GetAllClientsForAccount(accountId);
            var funders = await _claimSearchService.GetFunderInfoByIds(accountId);
            var staff = await _claimSearchService.GetStaffInfoByIds(accountId);
            var pos = await _claimSearchService.GetPlaceOfServiceInfoByIds(accountId);
            var locations = await _claimSearchService.GetLocationInfoByIds(accountId);

            return (
                clients ?? Enumerable.Empty<BaseNameOption>(),
                funders ?? Enumerable.Empty<BaseNameOption>(),
                staff ?? Enumerable.Empty<StaffBaseNameOption>(),
                pos ?? Enumerable.Empty<BaseNameOption>(),
                locations ?? Enumerable.Empty<BaseNameOption>()
            );
        }

        private List<string> ApplyFilter<T>(
            IEnumerable<T> source,
            IEnumerable<int> selectedIds,
            Func<T, int?> idSelector,
            Func<T, string> nameSelector)
        {
            if (source == null || !source.Any())
            {
                return new List<string>();
            }

            if (selectedIds != null && selectedIds.Any())
            {
                return source
                    .Where(x => idSelector(x).HasValue && selectedIds.Contains(idSelector(x).Value))
                    .Select(nameSelector)
                    .ToList();
            }

            return source.Select(nameSelector).ToList();
        }

        [HttpPost]
        public async Task<IActionResult> ExportUnprocessedAppointmentData([FromBody] UnprocessedAppointmentsRequestModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - ExportUnprocessedAppointmentData called. AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                nameof(AppointmentReportsController),
                nameof(ExportUnprocessedAppointmentData),
                model?.AccountInfoId, model?.MemberId);

            try
            {
                var filterForExport = await GetFiltersForExport(model);

                var exportModel = new ExportModelForUnprocessedAppointments
                {
                    Model = model,
                    Filter = filterForExport
                };

                var result = await _appointmentReportService.ExportUnprocessedAppointmentDataAsync(exportModel);
                var base64Excel = Convert.ToBase64String(result);
                return Ok(new { data = base64Excel });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(ExportUnprocessedAppointmentData)} - Failed to export unprocessed appointment data. AccountInfoId={model?.AccountInfoId}, MemberId={model?.MemberId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = "Failed to export data. Please try again." });
            }
        }

        /// <summary>
        /// Retrieves the count of unprocessed appointments for the specified account.
        /// </summary>
        /// <remarks>This method calls the underlying service to fetch the count of unprocessed
        /// appointments for the given account.  Ensure that the <paramref name="accountInfoId"/> corresponds to a valid
        /// account.</remarks>
        /// <param name="accountInfoId">The unique identifier of the account for which to retrieve the count of unprocessed appointments.</param>
        /// <returns>An <see cref="IActionResult"/> containing the count of unprocessed appointments if the operation is
        /// successful,  or a bad request response with an error message if the request is invalid.</returns>
        [HttpGet("{accountInfoId}")]
        public async Task<IActionResult> GetUnprocessedAppointmentsCount([FromRoute] int accountInfoId)
        {
            _logger.LogInformation("{Controller}.{Action} - GetUnprocessedAppointmentsCount called. AccountInfoId={AccountInfoId}",
                nameof(AppointmentReportsController),
                nameof(GetUnprocessedAppointmentsCount),
                accountInfoId);

            try
            {
                var appointments = await _appointmentReportService.UnprocessedAppointmentsCountAsync(accountInfoId);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetUnprocessedAppointmentsCount)} - Failed to get unprocessed appointments count. AccountInfoId={accountInfoId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = "Your request is invalid. Please check the input and try again." });
            }
        }


        /// <summary>
        /// Retrieves a list of unprocessed appointments based on the provided request model.
        /// </summary>
        /// <remarks>This method validates the input model and delegates the retrieval of unprocessed
        /// appointments to the underlying service. If the input is invalid or an error occurs during processing, a bad
        /// request response is returned with an appropriate error message.</remarks>
        /// <param name="model">The request model containing the criteria for retrieving unprocessed appointments. The <see
        /// cref="UnprocessedAppointmentsRequestModel.AccountInfoId"/> must be greater than 0.</param>
        /// <returns>An <see cref="IActionResult"/> containing the list of unprocessed appointments if the request is valid;
        /// otherwise, a bad request response with an error message.</returns>
        [HttpPost]
        public async Task<IActionResult> GetUnprocessedAppointments([FromBody] UnprocessedAppointmentsRequestModel model)
        {
            if (model.AccountInfoId <= 0)
            {
                _logger.LogWarning("{Controller}.{Action} - GetUnprocessedAppointments called with invalid AccountInfoId={AccountInfoId}",
                    nameof(AppointmentReportsController),
                    nameof(GetUnprocessedAppointments),
                    model?.AccountInfoId);
                return BadRequest(new { message = "Invalid or missing AccountInfoId." });
            }

            _logger.LogInformation("{Controller}.{Action} - GetUnprocessedAppointments called. AccountInfoId={AccountInfoId}, MemberId={MemberId}, StartDate={StartDate}, EndDate={EndDate}",
                nameof(AppointmentReportsController),
                nameof(GetUnprocessedAppointments),
                model.AccountInfoId, model.MemberId, model.StartDate, model.EndDate);

            try
            {
                var appointments = await _appointmentReportService.UnprocessedAppointments(model);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentReportsController)}.{nameof(GetUnprocessedAppointments)} - Failed to get unprocessed appointments. AccountInfoId={model.AccountInfoId}, ErrorMsg={ex.Message}");
                return BadRequest(new { message = "Your request is invalid. Please check the input and try again." });
            }
        }
    }
}
