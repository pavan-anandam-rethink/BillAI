using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using ClientService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace ClientService.Web.Controllers
{
    [Area("Client")]
    [Route("[controller]/[action]")]
    public class ClientController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly IClientService _clientService;
        private readonly ICommonService _commonService;
        private readonly ILogger<ClientController> _logger;

        public ClientController(
            IBaseHttpClient httpClient,
            IConfiguration configuration,
            IClientService clientService,
            ILogger<ClientController> logger,
            ICommonService commonService
            )
           : base(httpClient, configuration, logger)
        {
            _configuration = configuration;
            _clientService = clientService;
            _commonService = commonService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> GetClientsForClaim([FromBody] UserInfo model)
        {
            _logger.LogInformation("ClientController.GetClientsForClaim started | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}", model.AccountInfoId, model.MemberId);
            try
            {
                var clients = await _clientService.GetClientsListForClaimAsync(model.AccountInfoId, model.MemberId);
                _logger.LogInformation("ClientController.GetClientsForClaim completed | AccountInfoId: {AccountInfoId},MemberId: {MemberId}",
                    model.AccountInfoId, model.MemberId);

                return Ok(clients);
            }
            catch (Exception ex)
            {

                _logger.LogInformation("ClientController.GetClientsForClaim failed | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}",
                    model.AccountInfoId, model.MemberId);
                _logger.LogError(ex, "ClientController.GetClientsForClaim failed (error) | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}",
                    model.AccountInfoId, model.MemberId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> SearchDiagnosis([FromBody] SearchDiagnosisModel diagnosisSearchModel)
        {
            _logger.LogInformation("ClientController.SearchDiagnosis started | AccountInfoId: {AccountInfoId}, DiagnosisTypeId: {DiagnosisTypeId}, SearchTermLength: {SearchTermLength}",
                diagnosisSearchModel.AccountInfoId, diagnosisSearchModel.DiagnosisTypeId, diagnosisSearchModel.SearchTerm?.Length ?? 0);
            try
            {
                var result = await _clientService.SearchDiagnosis(diagnosisSearchModel.SearchTerm, diagnosisSearchModel.DiagnosisTypeId, diagnosisSearchModel.AccountInfoId);

                _logger.LogInformation("ClientController.SearchDiagnosis completed | AccountInfoId: {AccountInfoId}, ResultCount: {ResultCount},DiagnosisTypeId: {DiagnosisTypeId}, SearchTermLength: {SearchTermLength}",
                    diagnosisSearchModel.AccountInfoId, diagnosisSearchModel.DiagnosisTypeId, diagnosisSearchModel.SearchTerm?.Length ?? 0, result?.Count() ?? 0);
                return Ok(new { DiagnosisInfo = result, NoResults = result == null || !result.Any() });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "ClientController.SearchDiagnosis failed | AccountInfoId: {AccountInfoId}, DiagnosisTypeId: {DiagnosisTypeId}",
                    diagnosisSearchModel.AccountInfoId, diagnosisSearchModel.DiagnosisTypeId);
                _logger.LogInformation("ClientController.SearchDiagnosis ended with failure | AccountInfoId: {AccountInfoId}, DiagnosisTypeId: {DiagnosisTypeId},DiagnosisTypeId: {DiagnosisTypeId}, SearchTermLength: {SearchTermLength}",
                    diagnosisSearchModel.AccountInfoId, diagnosisSearchModel.DiagnosisTypeId, diagnosisSearchModel.SearchTerm?.Length ?? 0, diagnosisSearchModel.DiagnosisTypeId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimCreateInfo([FromBody] ClaimCreateInfoGetModel model)
        {
            _logger.LogInformation("ClientController.GetClaimCreateInfo started | AccountInfoId: {AccountInfoId}, ClientId: {ClientId}, FunderId: {FunderId}, ServiceId: {ServiceId}",
                model.AccountInfoId, model.ClientId, model.FunderId, model.ServiceId);
            try
            {
                var result = await _clientService.GetClaimCreateInfoAsync(model, model.AccountInfoId);
                _logger.LogInformation("ClientController GetClaimCreateInfo completed | AccountInfoId: {$AccountInfoId}",
                    model.AccountInfoId);

                return Ok(result);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "ClientController.GetClaimCreateInfo failed | AccountInfoId: {AccountInfoId}, ClientId: {ClientId}, ServiceId: {ServiceId},FunderId:{FunderId}",
                        model.AccountInfoId, model.ClientId, model.FunderId, model.ServiceId);

                _logger.LogInformation("ClientController.GetClaimCreateInfo ended with failure | AccountInfoId: {AccountInfoId}, ClientId: {ClientId}, ServiceId: {ServiceId}",
                        model.AccountInfoId, model.ClientId, model.ServiceId);

                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<ActionResult> GetDiagnosisForClaimWithoutAuth([FromBody] AuthDiagnosisRequest model)
        {
            _logger.LogInformation("ClientController.GetDiagnosisForClaimWithoutAuth started | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}, ServiceLineId: {ServiceLineId}, IncludeInactive: {IncludeInactive}",
                model.AccountInfoId, model.ChildProfileId, model.ServiceLineId, model.IncludeInactive);

            try
            {
                var clientDiagnosis = await _clientService.GetDiagnosisForClaimWithoutAuthAsync(model.ChildProfileId, model.ServiceLineId, model.AccountInfoId);


                _logger.LogInformation("ClientController.GetDiagnosisForClaimWithoutAuth completed | AccountInfoId: {AccountInfoId}, ResultCount: {ResultCount} ",
                         model.AccountInfoId, clientDiagnosis?.Count() ?? 0);

                return Ok(clientDiagnosis);
            }

            catch (Exception ex)
            {

                _logger.LogError(ex, "ClientController.GetDiagnosisForClaimWithoutAuth failed | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}, ServiceLineId: {ServiceLineId} ",
                    model.AccountInfoId, model.ChildProfileId, model.ServiceLineId);

                _logger.LogInformation("ClientController.GetDiagnosisForClaimWithoutAuth ended with failure | AccountInfoId: {AccountInfoId}, ServiceLineId: {ServiceLineId}",
                    model.AccountInfoId, model.ServiceLineId);

                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetFunderServiceLines([FromBody] FunderServiceLineRequestModel model)
        {

            _logger.LogInformation("ClientController.GetFunderServiceLines started | AccountInfoId: {AccountInfoId}, ClientId: {ClientId}, FunderId: {FunderId}, Id: {Id}",
                model.AccountInfoId, model.ClientId, model.FunderId, model.Id);

            try
            {
                var result = await _clientService.GetFunderServiceLinesAsync(model.Id, model.FunderId, model.AccountInfoId, model.ClientId);

                _logger.LogInformation("ClientController.GetFunderServiceLines completed | AccountInfoId: {AccountInfoId}, ResultCount: {ResultCount}",
                    model.AccountInfoId, result?.Count() ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {

                _logger.LogInformation("ClientController.GetFunderServiceLines failed (trace) | AccountInfoId: {AccountInfoId}, ClientId: {ClientId}, FunderId: {FunderId}, Id: {Id}",
                    model.AccountInfoId, model.ClientId, model.FunderId, model.Id);

                _logger.LogError(ex, "ClientController.GetFunderServiceLines failed (error)");

                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetClientFacilityId([FromBody] ClientFacilityIdModel model)
        {

            _logger.LogInformation("ClientController.GetClientFacilityId started | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}",
                model.AccountInfoId, model.childProfileId);

            try
            {
                var result = await _clientService.GetClientFacilityIdAsync(model.childProfileId, model.AccountInfoId);


                _logger.LogInformation("ClientController.GetClientFacilityId completed | AccountInfoId: {AccountInfoId}",
                        model.AccountInfoId);

                return Ok(result);
            }

            catch (Exception ex)
            {

                _logger.LogError(ex, "ClientController.GetClientFacilityId failed | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}",
                        model.AccountInfoId, model.childProfileId);

                _logger.LogInformation("ClientController.GetClientFacilityId ended with failure | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}",
                        model.AccountInfoId, model.childProfileId);

                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClientFunderResponsibleParties([FromBody] ClientFunderResponsiblePartyRequest body)
        {

            _logger.LogInformation("ClientController.GetClientFunderResponsibleParties started | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, ChildProfileId: {ChildProfileId}, ClientFunderId: {ClientFunderId}",
                body.AccountInfoId, body.MemberId, body.ChildProfileId, body.ClientFunderId);

            try
            {
                var result = await _clientService.GetClientFunderResponsiblePartiesAsync(body.MemberId, body.AccountInfoId, body.ChildProfileId, body.ClientFunderId);


                _logger.LogInformation("ClientController.GetClientFunderResponsibleParties completed | AccountInfoId: {AccountInfoId},MemberId: {MemberId},ChildProfileId: {ChildProfileId}, ClientFunderId: {ClientFunderId}",
                    body.AccountInfoId, body.MemberId, body.ChildProfileId, body.ClientFunderId);

                return Ok(result);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "ClientController.GetClientFunderResponsibleParties failed | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, ChildProfileId: {ChildProfileId}, ClientFunderId: {ClientFunderId}",
                    body.AccountInfoId, body.MemberId, body.ChildProfileId, body.ClientFunderId);

                _logger.LogInformation("ClientController.GetClientFunderResponsibleParties ended with failure | AccountInfoId: {AccountInfoId},MemberId: {MemberId},ChildProfileId: {ChildProfileId}, ClientFunderId: {ClientFunderId}",
                    body.AccountInfoId, body.MemberId, body.ChildProfileId, body.ClientFunderId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> GetClientAuthorization([FromBody] ClientAuthorizationRequest model)
        {


            _logger.LogInformation("ClientController.GetClientAuthorization started | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, ChildProfileId: {ChildProfileId}, AuthorizationId: {AuthorizationId}, Locale: {Locale}",
                model.AccountInfoId, model.MemberId, model.ChildProfileId, model.AuthorizationId, model.LocaleString);

            try
            {
                var result = await _clientService.GetClientAuthorization(model.AuthorizationId, model.ChildProfileId, model.MemberId, model.AccountInfoId, model.LocaleString);


                _logger.LogInformation("ClientController.GetClientAuthorization completed | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, ChildProfileId: {ChildProfileId}, AuthorizationId: {AuthorizationId}, Locale: {Locale}",
                model.AccountInfoId, model.MemberId, model.ChildProfileId, model.AuthorizationId, model.LocaleString);

                return Ok(result);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "ClientController.GetClientAuthorization failed | AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, ChildProfileId: {ChildProfileId}, AuthorizationId: {AuthorizationId}",
                    model.AccountInfoId, model.MemberId, model.ChildProfileId, model.AuthorizationId);

                _logger.LogInformation("ClientController.GetClientAuthorization ended with failure| AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, ChildProfileId: {ChildProfileId}, AuthorizationId: {AuthorizationId}, Locale: {Locale}",
                model.AccountInfoId, model.MemberId, model.ChildProfileId, model.AuthorizationId, model.LocaleString);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> GetClientAuthorizationsForClaim([FromBody] ClientAuthorizationsForClaimRequest body)
        {

            _logger.LogInformation("ClientController.GetClientAuthorizationsForClaim started | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}, FunderId: {FunderId}, ClientFunderServiceLineId: {ClientFunderServiceLineId}",
                body.AccountInfoId, body.ChildProfileId, body.FunderId, body.ClientFunderServiceLineId);

            try
            {
                var authorizations = await _clientService.GetClientAuthorizationsForClaimAsync(body.ChildProfileId, body.FunderId, body.ClientFunderServiceLineId, body.AccountInfoId);


                _logger.LogInformation("ClientController.GetClientAuthorizationsForClaim completed | AccountInfoId: {AccountInfoId}, ResultCount: {ResultCount}",
                    body.AccountInfoId, authorizations?.Count() ?? 0);

                return Ok(authorizations);
            }

            catch (Exception ex)
            {

                _logger.LogError(ex, "ClientController.GetClientAuthorizationsForClaim failed | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}, FunderId: {FunderId}, ClientFunderServiceLineId: {ClientFunderServiceLineId} ",
                       body.AccountInfoId, body.ChildProfileId, body.FunderId, body.ClientFunderServiceLineId);

                _logger.LogInformation("ClientController.GetClientAuthorizationsForClaim ended with failure | AccountInfoId: {AccountInfoId} ",
                    body.AccountInfoId);

                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClientFundersSmall([FromBody] ClientFundersSmallModel model)
        {
            try
            {
                _logger.LogInformation("ClientController.GetClientFunders started | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}",
                    model.AccountInfoId, model.childProfileId);
                var result = await _clientService.GetClientFundersAsync(model.childProfileId, model.AccountInfoId, true);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClientController.GetClientFunders failed | AccountInfoId: {AccountInfoId}, ChildProfileId: {ChildProfileId}",
                    model.AccountInfoId, model.childProfileId);
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetPlacesOfService([FromBody] UserInfo userInfo)
        {
            try
            {
                _logger.LogInformation("ClientController.GetPlacesOfService started | AccountInfoId: {AccountInfoId}", userInfo.AccountInfoId);
                var result = await _commonService.GetLocationCodes(userInfo.AccountInfoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClientController.GetPlacesOfService failed | AccountInfoId: {AccountInfoId}", userInfo.AccountInfoId);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
