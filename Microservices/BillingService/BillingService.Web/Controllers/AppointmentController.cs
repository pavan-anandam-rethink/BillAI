using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Threading.Tasks;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class AppointmentController : BaseController
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IClaimSyncService _claimSyncService;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(
            IAppointmentService appointmentService,
            IClaimSyncService claimSyncService,
            ILogger<AppointmentController> logger,
            IBaseHttpClient httpClient,
            IConfiguration configuration)
            : base(httpClient, configuration)
        {
            _appointmentService = appointmentService;
            _claimSyncService = claimSyncService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetForClaim([FromBody] IdWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                     "{Controller}.{Action} -Getting appointment for claim. MemberId={MemberId}, ClaimId={ClaimId}",
                     nameof(AppointmentController),
                     nameof(GetForClaim),
                     model.MemberId,
                     model.Id);
                var result = await _appointmentService.GetForClaim(
                    model.AccountInfoId,
                    model.MemberId,
                    model.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(GetForClaim)} -Failed to get appointment for claim. MemberId={model.MemberId}, claimId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetFor([FromBody] AppointmentGetRequest appointmentGetRequest)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} -Getting appointments. AccountId={AccountId}, MemberId={MemberId}, ClaimId={ClaimId}, ClientId={ClientId}, StartDate={StartDate}, EndDate={EndDate}, LocationId={LocationId}",
                    nameof(AppointmentController),
                    nameof(GetFor),
                    appointmentGetRequest.AccountInfoId,
                    appointmentGetRequest.MemberId,
                    appointmentGetRequest.ClaimId,
                    appointmentGetRequest.ClientId,
                    appointmentGetRequest.StartDate,
                    appointmentGetRequest.EndDate,
                    appointmentGetRequest.LocationId);

                var result = await _appointmentService.GetFor(
                    appointmentGetRequest.AccountInfoId,
                    appointmentGetRequest.MemberId,
                    appointmentGetRequest.ClaimId,
                    appointmentGetRequest.ClientId,
                    appointmentGetRequest.MemberId,
                    appointmentGetRequest.StartDate,
                    appointmentGetRequest.EndDate,
                    appointmentGetRequest.LocationId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(GetFor)} -Error getting appointments. MemberId={appointmentGetRequest.MemberId}, claimId={appointmentGetRequest.ClaimId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetClaimsAssignee([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                var result = await _appointmentService.GetClaimsAssignees(requestModel);

                if (result == null)
                    return StatusCode(502, "BH API returned no data");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(GetClaimsAssignee)} -Failed to get claim assignees. ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LinkAppointments([FromBody] LinkAppointmentsRequest linkAppointmentsRequestWithUserInfo)
        {
            //if (!CheckPermissions(PermissionLevelEnum.BillingAddEditApproveSubmitClaims))
            //{
            //    return Forbid();
            //}
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} -Linking appointments. AccountId={AccountId}, MemberId={MemberId}, ClaimId={ClaimId}, AppointmentCount={AppointmentCount}",
                    nameof(AppointmentController),
                    nameof(LinkAppointments),
                    linkAppointmentsRequestWithUserInfo.AccountInfoId,
                    linkAppointmentsRequestWithUserInfo.MemberId,
                    linkAppointmentsRequestWithUserInfo.ClaimId,
                    linkAppointmentsRequestWithUserInfo.AppointmentIds?.Count ?? 0);

                var result = await _appointmentService.LinkAppointments(
                    linkAppointmentsRequestWithUserInfo.AccountInfoId,
                    linkAppointmentsRequestWithUserInfo.MemberId,
                    linkAppointmentsRequestWithUserInfo.ClaimId,
                    linkAppointmentsRequestWithUserInfo.AppointmentIds);


                return Ok(new { Success = result.Item1, StartDate = result.Item2, EndDate = result.Item3 });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(LinkAppointments)} -Failed to link appointments. AccountId={linkAppointmentsRequestWithUserInfo.AccountInfoId}, MemberId={linkAppointmentsRequestWithUserInfo.MemberId}, ClaimId={linkAppointmentsRequestWithUserInfo.ClaimId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnLinkAppointments([FromBody] LinkAppointmentsRequest linkAppointmentsRequestWithUserInfo)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} -Unlinking appointments. AccountId={AccountId}, MemberId={MemberId}, ClaimId={ClaimId}, AppointmentCount={AppointmentCount}",
                    nameof(AppointmentController),
                    nameof(UnLinkAppointments),
                    linkAppointmentsRequestWithUserInfo.AccountInfoId,
                    linkAppointmentsRequestWithUserInfo.MemberId,
                    linkAppointmentsRequestWithUserInfo.ClaimId,
                    linkAppointmentsRequestWithUserInfo.AppointmentIds?.Count ?? 0);

                var result = await _appointmentService.UnLinkAppointments(
                    linkAppointmentsRequestWithUserInfo.AccountInfoId,
                    linkAppointmentsRequestWithUserInfo.MemberId,
                    linkAppointmentsRequestWithUserInfo.ClaimId,
                    linkAppointmentsRequestWithUserInfo.AppointmentIds);


                return Ok(new { Success = result.Item1, StartDate = result.Item2, EndDate = result.Item3 });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(UnLinkAppointments)} -Failed to unlink appointments. AccountId={linkAppointmentsRequestWithUserInfo.AccountInfoId}, MemberId={linkAppointmentsRequestWithUserInfo.MemberId}, ClaimId={linkAppointmentsRequestWithUserInfo.ClaimId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncClaim([FromBody] AutoClaimRequestModel autoClaimRequest)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} -Syncing claim for appointment. AppointmentId={AppointmentId}, AccountId={AccountId}",
                    nameof(AppointmentController),
                    nameof(SyncClaim), autoClaimRequest.appointmentId,
                    autoClaimRequest.accountId);

                await _claimSyncService.SyncClaimAsync(autoClaimRequest.appointmentId, autoClaimRequest.accountId, autoClaimRequest.processingSchedule ?? false);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(SyncClaim)} -Error creating claim for appointment.Where appointmentId={autoClaimRequest.appointmentId}, accountId={autoClaimRequest.accountId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncClaimDelete([FromBody] int appointmentId)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} -Deleting synced claim for appointment. AppointmentId={AppointmentId}",
                    nameof(AppointmentController),
                    nameof(SyncClaimDelete),
                    appointmentId);

                await _claimSyncService.SyncClaimDeleteAsync(appointmentId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(SyncClaimDelete)} -Failed to delete synced claim for appointment. AppointmentId={appointmentId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> AutoProcessUnBilledAppointmentSchedule()
        {
            _logger.LogInformation("{Controller}.{Action} - AutoProcessUnBilledAppointmentSchedule called.",
                    nameof(AppointmentController),
                    nameof(AutoProcessUnBilledAppointmentSchedule));

            try
            {
                await _claimSyncService.AutoProcessUnBilledAppointmentScheduleBatchAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(AppointmentController)}.{nameof(AutoProcessUnBilledAppointmentSchedule)} - Failed to process unbilled for unbilled appointments.");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}