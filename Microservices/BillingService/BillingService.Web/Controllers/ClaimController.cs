using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ClaimController : BaseController
    {
        private readonly ILogger<ClaimController> _logger;
        private readonly IClaimService _claimService;
        private readonly IProviderLocationService _providerLocationService;
        private readonly IMemberAccountService _memberAccountService;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IClaimVersionService _claimVersionService;
        private readonly IClaimCreateService _claimCreateService;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly ICommonService _commonService;
        private readonly IClientService _clientService;
        private readonly IKeyVaultProviderService _keyVaultProviderService;
        private IConfiguration _configuration;

        public ClaimController(
            IBaseHttpClient httpClient,
            IConfiguration configuration,
            ILogger<ClaimController> logger,
            IClaimService claimService,
            IProviderLocationService providerLocationService,
            IMemberAccountService memberAccountService,
            IClaimHistoryService claimHistoryService,
            IClaimVersionService claimVersionService,
            IClaimCreateService claimCreateService,
            IRethinkMasterDataMicroServices rethinkMasterDataMicroServices,
            ICommonService commonService,
            IClientService clientService,
            IKeyVaultProviderService keyVaultProviderService)
            : base(httpClient, configuration)
        {
            _clientService = clientService;
            _logger = logger;
            _claimService = claimService;
            _providerLocationService = providerLocationService;
            _memberAccountService = memberAccountService;
            _claimHistoryService = claimHistoryService;
            _claimVersionService = claimVersionService;
            _claimCreateService = claimCreateService;
            _rethinkServices = rethinkMasterDataMicroServices;
            _commonService = commonService;
            _keyVaultProviderService = keyVaultProviderService;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> GetOptions([FromBody] ClaimIdWithUserInfo userInfo)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Getting claim options. AccountInfoId={AccountInfoId}, MemberId={MemberId}",
                  nameof(ClaimController),
                    nameof(GetOptions),
                    userInfo.AccountInfoId, userInfo.MemberId);
                ClaimOptions options = new ClaimOptions();

                var clientList = await _clientService.GetClientsListForClaimAsync(userInfo.AccountInfoId, userInfo.MemberId);

                options.Clients = clientList.Select(cl => new BasicOption()
                {
                    Id = cl.Id,
                    Name = cl.Name
                }).ToList();

                var locations = await _providerLocationService.GetForAccount(userInfo.AccountInfoId);
                options.Locations = locations.Select(l => new BasicOption
                {
                    Id = l.id,
                    Name = l.name
                }).ToList();
                options.Locations.Add(new BasicOption
                {
                    Id = 1,
                    Name = "Staff Home"
                });
                options.Locations.Add(new BasicOption
                {
                    Id = 2,
                    Name = "Client Home"
                });

                var members = await _memberAccountService.GetMembersByAccountInfoId(userInfo.AccountInfoId);
                options.Members = members.Select(m => new BasicOption()
                {
                    Id = m.Id,
                    Name = m.FirstName + " " + m.LastName
                }).ToList();

                var locationCodes = await _commonService.GetLocationCodes(userInfo.AccountInfoId);
                options.LocationCodes = locationCodes.Select(lc => new BasicOption()
                {
                    Id = lc.Id,
                    Name = lc.Code + " - " + lc.Description
                }).ToList();

                options.RenderingProviders = await _claimService.GetClaimRenderingProviders(userInfo.AccountInfoId);
                options.ReferringProviders = await _claimService.GetClaimReferringProviders(userInfo.Id, userInfo.AccountInfoId);
                options.ServiceFacilities = locations.Select(x => new BasicOption { Id = x.id, Name = x.name }).OrderBy(x => x.Name).ToList();
                options.BillingProviders = locations.Where(x => x.isBillingLocation).Select(x => new BasicOption { Id = x.id, Name = x.name + " - " + x.agencyName }).OrderBy(x => x.Name).ToList();

                var claimIds = await _claimService.GetIdsForAccountAsync(userInfo.AccountInfoId);
                options.ClaimIds = claimIds;

                var unitTypes = await _rethinkServices.GetUnitTypesAsync();
                options.UnitTypes = unitTypes.Select(x => new BasicOption { Id = x.id, Name = x.unitString }).ToList();

                _logger.LogInformation("Successfully retrieved claim options. AccountInfoId={AccountInfoId}, MemberId={MemberId}",
              userInfo.AccountInfoId, userInfo.MemberId);

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetOptions)} -Failed to get claim options. AccountInfoId={userInfo.AccountInfoId}, MemberId={userInfo.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetBillingClaimDetails([FromBody] GetBillingClaimDetailsModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetBillingClaimDetails called. ClaimId={ClaimId}",
                nameof(ClaimController),
                nameof(GetBillingClaimDetails),
                model.ClaimId);

            try
            {
                var result = await _claimService.GetClaimChargesForAccountAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetBillingClaimDetails)} -GetBillingClaimDetails failed. ClaimId={model.ClaimId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveBillingClaimDetails([FromBody] RemoveBillingClaimDetailsModel model)
        {

            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Removing billing claim details.MemberId={MemberId}",
                      nameof(ClaimController),
                      nameof(RemoveBillingClaimDetails), model.MemberId);

                var result = await _claimService.RemoveBillingClaimDetailAsync(model);

                _logger.LogInformation("Successfully removed billing claim details.");


                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(RemoveBillingClaimDetails)} -Error removing billing claim details.MemberId={model.MemberId}, Error={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessClaimCreation([FromBody] ClaimCreateEnd model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Processing claim creation. ClaimId={ClaimId}, ClientId={ClientId}",
                              nameof(ClaimController),
                              nameof(ProcessClaimCreation),
                              model.ClaimId, model.ClientId);

                await _claimCreateService.ProcessClaimCreation(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(ProcessClaimCreation)} -Failed to process claim creation. ClaimId={model.ClaimId}, ClientId={model.ClientId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClaimDetails([FromBody] UpdateDetails model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Updating claim details",
                     nameof(ClaimController),
                     nameof(UpdateClaimDetails));

                var result = await _claimService.UpdateClaimAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(UpdateClaimDetails)} -Error updating claim details.MemberId={model.MemberId}, Error={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBillingClaimDetails([FromBody] UpdateBillingClaimDetailsListModel model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Updating billing claim details. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(UpdateBillingClaimDetails), model.MemberId);

                var result = await _claimService.UpdateBillingClaimAsync(model, model.MemberId, true);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(UpdateBillingClaimDetails)} -Error updating billing claim details. MemberId={model.MemberId}, Error={ex.Message}");

                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// Retrieves a list of claims based on the specified sort, filter, and user information.
        /// </summary>
        /// <remarks>This method determines whether to use the new claim processing endpoint based on a
        /// configuration value retrieved from Key Vault. The flag indicating the use of the new claim processing is
        /// applied to each claim header in the result.</remarks>
        /// <param name="requestModel">The request model containing sort, filter, and user information for retrieving claim headers.</param>
        /// <returns>An <see cref="IActionResult"/> containing the claim headers that match the specified criteria. Returns an
        /// HTTP 200 status code with the result if successful, or an HTTP 400 status code with an error message if an
        /// exception occurs.</returns>
        /// <response code="200">Success, Get Claims headers</response>
        /// <response code="400">Validation error. See response for details</response>
        /// <response code="401">Authentication missing</response>
        /// <response code="403">Forbidden - caller not authorized</response>
        [HttpPost]
        public async Task<IActionResult> GetClaimHeaders([FromBody] ClaimGetRequestSortFilterWithUserInfo requestModel)
        {
            try
            {
                _logger.LogInformation(
                        "{Controller}.{Action} - Getting claim headers. MemberId={MemberId}",
                          nameof(ClaimController),
                          nameof(GetClaimHeaders),
                          requestModel.MemberId);

                // Check if call the new ClaimProcessing Endpoint of old one
                var key = _configuration["UseNewClaimProcessing"];
                var getDataFromKv = string.IsNullOrEmpty(key) ? string.Empty : await _keyVaultProviderService.GetSecretAsync(key).ConfigureAwait(false);
                var useNewClaimProcessing = getDataFromKv.Length > 0 ? Convert.ToBoolean(getDataFromKv) : false;

                // get the data from the service
                var result = await _claimService.GetClaimHeadersAsync(requestModel);

                // set the flag to each claim header
                if (useNewClaimProcessing)
                {
                    result?.Data?.ForEach(claimHeader =>
                    {
                        claimHeader.UseNewClaimProcessing = useNewClaimProcessing;
                    });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimHeaders)} -Error getting claim headers. memberId={requestModel.MemberId}\n Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCarcCodes()
        {
            _logger.LogInformation("{Controller}.{Action} - GetAllCarcCodes called.",
                nameof(ClaimController),
                nameof(GetAllCarcCodes));

            try
            {
                var result = await _claimService.GetAllCarcCodes();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetAllCarcCodes)} -GetAllCarcCodes failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimDetails([FromBody] IdWithUserInfo requestModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim details. ClaimId={ClaimId}, MemberId={MemberId}",
                     nameof(ClaimController),
                     nameof(GetClaimDetails),
                     requestModel.Id,
                     requestModel.MemberId);

                var result = await _claimService.GetClaimDetailsAsync(requestModel, null);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimDetails)} -Error getting claim details. ClaimId={requestModel.Id}, MemberId={requestModel.MemberId}, Error={ex.Message}");

                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetHFCAClaimDetails([FromBody] IdsWithUserInfo requestModel)
        {
            _logger.LogInformation("{Controller}.{Action} - GetHFCAClaimDetails called. MemberId={MemberId}, AccountInfoId={AccountInfoId}, Ids={Ids}",
                 nameof(ClaimController), nameof(GetHFCAClaimDetails), requestModel.MemberId, requestModel.AccountInfoId, string.Join(",", requestModel.Ids));

            try
            {
                var result = await _claimService.GetHFCAClaimDetailsAsync(requestModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetHFCAClaimDetails)} - Failed to get HFCA claim details. MemberId={requestModel.MemberId}, AccountInfoId={requestModel.AccountInfoId}, Ids={string.Join(", ", requestModel.Ids)}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetClaimLineAppointments([FromBody] ServiceLineIdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetClaimLineAppointments called. AccountInfoId={AccountInfoId}, ServiceLineId={ServiceLineId}",
                nameof(ClaimController),
                nameof(GetClaimLineAppointments),
                model.AccountInfoId,
                model.ServiceLineId);

            try
            {
                var result = await _claimService.GetClaimLineAppointmentsAsync(model.AccountInfoId, model.ServiceLineId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimLineAppointments)} -GetClaimLineAppointments failed. AccountInfoId={model.AccountInfoId}, ServiceLineId={model.ServiceLineId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimPatients([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Getting claim patients.",
                  nameof(ClaimController),
                nameof(GetClaimPatients));

                var result = await _claimService.GetClaimPatientsAsync(requestModel);


                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimPatients)} -Failed to get claim patients. ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimFunders([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim funders. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetClaimFunders),
                    requestModel.MemberId);

                var result = await _claimService.GetClaimFundersAsync(requestModel);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimFunders)} -Failed to get claim funders. MemberId={requestModel.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimRenderingProviders([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim rendering providers. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetClaimRenderingProviders),
                    requestModel.MemberId);

                var result = await _claimService.GetClaimRenderingProvidersAsync(requestModel);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimRenderingProviders)} -Failed to get claim rendering providers. MemberId={requestModel.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimTabStatuses([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim tab statuses. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetClaimTabStatuses),
                    requestModel.MemberId);

                var result = await _claimService.GetClaimTabStatusesAsync(requestModel);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimTabStatuses)} -Failed to get claim tab statuses. MemberId={requestModel.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimIdentifiers([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim identifiers. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetClaimIdentifiers),
                    requestModel.MemberId);

                var result = await _claimService.GetClaimIdentifiersAsync(requestModel);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimIdentifiers)} -Failed to get claim identifiers. MemberId={requestModel.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Get([FromBody] ClaimIdWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim by identifier. ClaimIdentifier={ClaimIdentifier}, AccountInfoId={AccountInfoId}",
                    nameof(ClaimController),
                    nameof(Get),
                    model.ClaimIdentifier,
                    model.AccountInfoId);

                var result = await _claimService.GetClaimByIdentifierAsync(model.ClaimIdentifier, model.AccountInfoId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(Get)} -Failed to get claim by identifier. ClaimIdentifier={model.ClaimIdentifier}, AccountInfoId={model.AccountInfoId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAccountClaims([FromBody] ClaimSearchModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting account claims. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetAccountClaims),
                    model.MemberId);

                var result = await _claimService.GetAccountClaimByIdOrPatientNameAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetAccountClaims)} -Failed to get account claims. MemberId={model.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> IsDiagnosisServiceLineHasActiveClaims([FromBody] IsDiagnosisInUseModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Checking diagnosis usage by claims. ClientId={ClientId}, DiagnosisCodeId={DiagnosisCodeId}",
                    nameof(ClaimController),
                    nameof(IsDiagnosisServiceLineHasActiveClaims),
                    model.ClientId,
                    model.DiagnosisCodeId);

                var result = await _claimService.IsDiagnosisServiceLineHasActiveClaims(
                    model.ClientId,
                    model.DiagnosisCodeId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(IsDiagnosisServiceLineHasActiveClaims)} -Failed to check diagnosis usage. ClientId={model.ClientId}, DiagnosisCodeId={model.DiagnosisCodeId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDiagnosisServiceLineUsedByClaims([FromBody] IsDiagnosisInUseModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting diagnosis service line usage. ClientId={ClientId}, DiagnosisCodeId={DiagnosisCodeId}",
                    nameof(ClaimController),
                    nameof(GetDiagnosisServiceLineUsedByClaims),
                    model.ClientId,
                    model.DiagnosisCodeId);

                var result = await _claimService.GetDiagnosisServiceLineUsedByClaims(
                    model.ClientId,
                    model.DiagnosisCodeId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetDiagnosisServiceLineUsedByClaims)} -Failed to get diagnosis service line usage. ClientId={model.ClientId}, DiagnosisCodeId={model.DiagnosisCodeId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSelectedColumns([FromBody] MemberViewSettingWithUserInfo memberViewModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Saving selected columns. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(SaveSelectedColumns),
                    memberViewModel.MemberId);

                var result = await _claimService.SaveSelectedColumnsAsync(
                    memberViewModel.AccountInfoId,
                    memberViewModel.MemberId,
                    memberViewModel.SelectedColumns);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(SaveSelectedColumns)} -Failed to save selected columns. MemberId={memberViewModel.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetMemberViewSettings([FromBody] UserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting member view settings. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetMemberViewSettings),
                    model.MemberId);

                var result = await _claimService.GetMemberViewSettingsAsync(model.MemberId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetMemberViewSettings)} -Failed to get member view settings. MemberId={model.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimHistory([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation(
                   "{Controller}.{Action} - GetClaimHistory called. ClaimId={ClaimId}, MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetClaimHistory),
                    model.Id,
                    model.MemberId);
            try
            {
                var result = await _claimHistoryService.GetAllAsync(
                    model.Id,
                    model.AccountInfoId,
                    model.MemberId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimHistory)} -Failed to get claim history. ClaimId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> ValidateClaimData([FromBody] IdsWithUserInfo model)
        //{
        //    var validatedClaimIds = new List<int>();
        //    foreach (var claimId in model.ClaimsIds)
        //    {
        //        try
        //        {
        //            await _claimService.ValidateClaimDataAsync(claimId, model.MemberId);
        //            validatedClaimIds.Add(claimId);
        //        }
        //        catch (Exception) { }
        //    }
        //    return Ok(validatedClaimIds);
        //}

        [HttpPost]
        public async Task<IActionResult> ValidateClaimData([FromBody] ClaimValidationModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Validating claim data. MemberId={MemberId}, ClaimId={ClaimId}",
                    nameof(ClaimController),
                    nameof(ValidateClaimData),
                    model.MemberId,
                    model.Id);

                await _claimService.ValidateClaimDataAsync(model);

                return Ok(model.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(ValidateClaimData)} - Error validating claim: memberId={model.MemberId}, claimId={model.Id}, Error: {ex.Message}");
                return Ok(-1);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidateFunderChangedClaimsData([FromBody] ClaimFunderChangedModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Validating claims after funder change. MemberId={MemberId}, ClaimId={ClaimId}, ClientFunderId={ClientFunderId}",
                    nameof(ClaimController),
                    nameof(ValidateFunderChangedClaimsData),
                    model.MemberId,
                    model.Id,
                    model.ClientFunderId);

                await _claimService.ValidateClaimsOnFunderChangedAsync(
                    model.Id,
                    model.ClientFunderId,
                    model.FunderModifiedDate,
                    model.MemberId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(ValidateFunderChangedClaimsData)} - Failed to validate claims after funder change. MemberId={model.MemberId}, ClaimId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimErrorsAndAlerts([FromBody] IdWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim errors and alerts. ClaimId={ClaimId}",
                    nameof(ClaimController),
                    nameof(GetClaimErrorsAndAlerts),
                    model.Id);

                var result = await _claimService.GetClaimErrorsAndAlertsAsync(model.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimErrorsAndAlerts)} - Failed to get claim errors and alerts. ClaimId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetErrorsSources([FromBody] UserInfo userInfo)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting error sources.",
                    nameof(ClaimController),
                    nameof(GetErrorsSources));

                var result = await _claimService.GetErrorsSourcesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetErrorsSources)} - Failed to get error sources. ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetErrorsCodes([FromBody] UserInfo userInfo)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting error codes.",
                    nameof(ClaimController),
                    nameof(GetErrorsCodes));

                var result = await _claimService.GetErrorsCodesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetErrorsCodes)} - Failed to get error codes. ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveClaim([FromBody] ClaimSaveModelWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Saving claim. MemberId={MemberId}, AccountInfoId={AccountInfoId}",
                    nameof(ClaimController),
                    nameof(SaveClaim),
                    model.MemberId,
                    model.AccountInfoId);

                var result = await _claimService.SaveClaimAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(SaveClaim)} - Failed to save claim. MemberId={model.MemberId}, ErrorMsg={ex.Message}");
                return BadRequest(ex.StackTrace);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveClaims([FromBody] IdsWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Approving claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(ApproveClaims),
                    model.MemberId, string.Join(",", model.Ids));

                var result = await _claimService.ApproveClaimsAsync(
                    model.AccountInfoId, model.MemberId, model.Ids);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(ApproveClaims)} - Failed to approve claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnapproveClaims([FromBody] IdsWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Unapproving claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(UnapproveClaims),
                    model.MemberId, string.Join(",", model.Ids));

                var result = await _claimService.UnapproveClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.Ids);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(UnapproveClaims)} - Failed to unapprove claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> FlagClaims([FromBody] UnflagImperson model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Flagging claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(FlagClaims),
                    model.MemberId, string.Join(",", model.Ids));

                var result = await _claimService.FlagClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.Ids,
                    model.Rethinkuser);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(FlagClaims)} - Failed to flag claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> FlagClaimsWithReasons([FromBody] FlagClaimsRequest model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Flagging claims with reasons. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(FlagClaimsWithReasons),
                    model.MemberId);

                if (model.ClaimIds == null || !model.ClaimIds.Any())
                    return BadRequest("At least one claim is required.");

                if (model.Reasons == null || !model.Reasons.Any())
                    return BadRequest("At least one reason is required.");

                var reasonIds = model.Reasons.Select(r => r.ReasonId).ToArray();

                var flaggedIds = await _claimService.FlagClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.ClaimIds.ToArray(),
                    reasonIds,
                    model.Notes,
                    model.ClaimFlagTransactionId,
                    model.ImpersonationUserName);

                return Ok(flaggedIds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(FlagClaimsWithReasons)} - Error flagging claims: memberId={model.MemberId}, Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnflagClaims([FromBody] UnflagImperson model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Unflagging claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(UnflagClaims),
                    model.MemberId, string.Join(",", model.Ids));

                var result = await _claimService.UnflagClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.Ids,
                    model.Rethinkuser);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(UnflagClaims)} - Failed to unflag claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClaims([FromBody] DeleteClaimsInfo model)
        {
            try
            {                
                _logger.LogInformation(
                    "{Controller}.{Action} - Deleting claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(DeleteClaims),
                    model.MemberId, string.Join(",", model.Ids));

                var result = await _claimService.DeleteClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.Ids,
                    model.ImpersonationUserName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(DeleteClaims)} -Failed to delete claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitClaims([FromBody] ClaimsSubmitModel model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Submitting claims. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(SubmitClaims),
                    model.MemberId);

                var result = await _claimService.SubmitClaimsAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(SubmitClaims)} -Error submitting claims. memberId={model.MemberId}, claimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkBilledClaims([FromBody] IdsWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Marking claims as billed. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(MarkBilledClaims),
                    model.MemberId, string.Join(",", model.Ids));

                var result = await _claimService.MarkBilledClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.Ids);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(MarkBilledClaims)} -Failed to mark claims as billed. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> VoidClaims([FromBody] ClaimsVoidModelWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Voiding claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(VoidClaims),
                    model.MemberId, string.Join(",", model.ClaimsToVoid));

                var result = await _claimService.VoidClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.ClaimsToVoid,
                    0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(VoidClaims)} -Failed to void claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.ClaimsToVoid)}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteClaims([FromBody] IdsWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Completing claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(CompleteClaims),
                    model.MemberId, string.Join(",", model.Ids));

                var result = await _claimService.CompleteSelectedClaimsAsync(
                    model.Ids,
                    model.AccountInfoId,
                    model.MemberId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(CompleteClaims)} -Failed to complete claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RebillClaims([FromBody] ClaimsRebillModelWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Rebilling claims. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(RebillClaims),
                    model.MemberId, string.Join(",", model.ClaimsToRebill));

                var result = await _claimService.RebillClaimsAsync(
                    model.AccountInfoId,
                    model.MemberId,
                    model.ClaimsToRebill,
                    0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(RebillClaims)} -Failed to rebill claims. MemberId={model.MemberId}, ClaimIds={string.Join(",", model.ClaimsToRebill)}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SecondaryBillingRebillClaims([FromBody] SecondaryBillingClaimsRebillModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Starting secondary billing rebill. MemberId={MemberId}, DetailsCount={DetailsCount}",
                    nameof(ClaimController),
                    nameof(SecondaryBillingRebillClaims),
                    model.MemberId, model.SecondaryFunderDetails?.Count ?? 0);

                var result = await _claimService.SecondaryBillingRebillClaimsAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(SecondaryBillingRebillClaims)} -Failed secondary billing rebill. MemberId={model.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimBillNextFunders([FromBody] IdWithUserInfo model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Getting next bill funders. MemberId={MemberId}, ClaimId={ClaimId}",
                                nameof(ClaimController),
                                nameof(GetClaimBillNextFunders),
                                model.MemberId, model.Id);

                var result = await _claimService.GetClaimBillNextFundersAndControlNumberAsync(model.AccountInfoId, model.MemberId, model.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimBillNextFunders)} - Error getting next bill funders. memberId={model.MemberId}, claimId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimProviderLocationUsageCount([FromBody] int providerLocationId)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting provider location usage count. ProviderLocationId={ProviderLocationId}",
                    nameof(ClaimController),
                    nameof(GetClaimProviderLocationUsageCount),
                    providerLocationId);

                var result = await _claimService.ClaimProviderLocationUsageCountAsync(providerLocationId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(GetClaimProviderLocationUsageCount)} -Failed to get provider location usage count. ProviderLocationId={providerLocationId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimReferringProviderUsageCount([FromBody] int providerId)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting referring provider usage count. ProviderId={ProviderId}",
                    nameof(ClaimController),
                    nameof(GetClaimReferringProviderUsageCount),
                    providerId);

                var result = await _claimService.ClaimReferringProviderUsageCountAsync(providerId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(GetClaimReferringProviderUsageCount)} -Failed to get referring provider usage count. ProviderId={providerId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimStaffAsRendingProviderUsageCount([FromBody] int staffId)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting staff rendering provider usage count. StaffId={StaffId}",
                    nameof(ClaimController),
                    nameof(GetClaimStaffAsRendingProviderUsageCount),
                    staffId);

                var result = await _claimService.ClaimStaffAsRendingProviderUsageCountAsync(staffId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(GetClaimStaffAsRendingProviderUsageCount)} -Failed to get staff rendering provider usage count. StaffId={staffId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckFunderUsageByBilledClaims([FromBody] ClientFunderModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Checking funder usage by billed claims. ClientFunderId={ClientFunderId}",
                    nameof(ClaimController),
                    nameof(CheckFunderUsageByBilledClaims),
                    model.FunderId);

                var result = await _claimService.HasFunderBilledClaimsAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(CheckFunderUsageByBilledClaims)} -Failed to check funder usage by billed claims. ClientFunderId={model.FunderId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetEditAuthWarning([FromBody] AuthorizationModifiedModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Setting edit authorization warning. AuthorizationId={AuthorizationId}, MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(SetEditAuthWarning),
                    model.AuthorizationId,
                    model.MemberId);

                await _claimService.SetEditAuthWarningAsync(model);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(SetEditAuthWarning)} -Failed to set edit authorization warning. AuthorizationId={model.AuthorizationId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckIsAuthInUseByClaim([FromBody] int authorizationId)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Checking if authorization is in use by claim. AuthorizationId={AuthorizationId}",
                    nameof(ClaimController),
                    nameof(CheckIsAuthInUseByClaim),
                    authorizationId);

                var authInUse = await _claimService.CheckIsAuthUsedByClaimAsync(authorizationId);

                return Ok(authInUse);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(CheckIsAuthInUseByClaim)} -Failed to check authorization usage by claim. AuthorizationId={authorizationId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimHistoryVersion([FromBody] IdWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim history version. ClaimId={ClaimId}",
                    nameof(ClaimController),
                    nameof(GetClaimHistoryVersion),
                    model.Id);

                var result = await _claimVersionService.GetByIdAsync(model.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(GetClaimHistoryVersion)} -Failed to get claim history version. ClaimId={model.Id}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PropagateProvidersClaimData([FromBody] PropagatingProvidersClaimDataModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Propagating providers claim data.",
                    nameof(ClaimController),
                    nameof(PropagateProvidersClaimData));

                var result = await _claimService.PopagateProvidersClaimDataAsync(model, model.AccountInfoId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(PropagateProvidersClaimData)} -Failed to propagate providers claim data. ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> IsFunderHasActiveClaims([FromBody] IsClientFundersInUseModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Checking if funder has active claims. ClientFunderId={ClientFunderId}",
                    nameof(ClaimController),
                    nameof(IsFunderHasActiveClaims),
                    string.Join(",", model.ClientFunderIds));

                var result = await _claimService.IsFunderHasActiveClaimsAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(IsFunderHasActiveClaims)} -Failed to check funder active claims. ClientFunderId={string.Join(",", model.ClientFunderIds)}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetClaimHistoryActions([FromBody] UserInfo userInfo)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Getting claim history actions.",
                    nameof(ClaimController),
                    nameof(GetClaimHistoryActions));

                var result = await _claimHistoryService.GetClaimHistoryActionsAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimHistoryActions)} -Failed to get claim history actions. ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGridPageSizes()
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Getting grid page sizes.",
                    nameof(ClaimController),
                    nameof(GetGridPageSizes));

                var pageSizesTask = _keyVaultProviderService.GetSecretAsync(_configuration["GridPageSizes"]);
                var defaultPageSizeTask = _keyVaultProviderService.GetSecretAsync(_configuration["DefaultPageSize"]);
                await Task.WhenAll(pageSizesTask, defaultPageSizeTask).ConfigureAwait(false);
                var pageSizesSecret = await pageSizesTask.ConfigureAwait(false);
                var pageSizes = JsonSerializer.Deserialize<object[]>(pageSizesSecret);

                var defaultPageSizeSecret = await defaultPageSizeTask.ConfigureAwait(false);
                var defaultPageSize = JsonSerializer.Deserialize<object>(defaultPageSizeSecret);

                var response = new
                {
                    PageSizes = pageSizes,
                    DefaultPageSize = defaultPageSize
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetGridPageSizes)} -Failed to get grid page sizes. ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{accountInfoId}")]
        public async Task<IActionResult> GetRenderingProvidersForAccount(int accountInfoId)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting rendering providers for account. AccountInfoId={AccountInfoId}",
                    nameof(ClaimController),
                    nameof(GetRenderingProvidersForAccount),
                    accountInfoId);

                var providers = await _claimService.GetRenderingProviders(accountInfoId);

                return Ok(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(GetRenderingProvidersForAccount)} -Failed to get rendering providers. AccountInfoId={accountInfoId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClaimStatus([FromBody] UpdateClaimRequestModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Updating claim status. MemberId={MemberId}, ClaimId={ClaimId}",
                    nameof(ClaimController),
                    nameof(UpdateClaimStatus),
                    model.MemberId, model.ClaimId);

                await _claimService.UpdateClaimsStatusAsync(model);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(UpdateClaimStatus)} -Failed to update claim status. MemberId={model.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetStaffLocations([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting staff locations. MemberId={MemberId}",
                    nameof(ClaimController),
                    nameof(GetStaffLocations),
                    requestModel.MemberId);

                var locations = await _claimService.GetStaffLocations(requestModel);

                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(GetStaffLocations)} -Failed to get staff locations. MemberId={requestModel.MemberId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Submit claim to service bus for processing
        /// </summary>
        /// <remarks>Submits claims to the service bus for Async processing based on the provided request model.</remarks>
        /// <param name="requestModel">Request mode for claim submission</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the claim submission.
        /// </returns>
        /// <response code="204">Success, Submit claim processed</response>
        /// <response code="400">Validation error. See response for details</response>
        /// <response code="401">Authentication missing</response>
        /// <response code="403">Forbidden - caller not authorized</response>
        [HttpPost]
        public async Task<IActionResult> SubmitClaimToServiceBus([FromBody] ClaimsSubmitModel requestModel)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Submitting claims to service bus. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(SubmitClaimToServiceBus),
                    requestModel.MemberId,
                    string.Join(",", requestModel.Ids));

                await _claimService.SubmitClaimsToServiceBusAsync(requestModel);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{nameof(ClaimController)}.{nameof(SubmitClaimToServiceBus)} -Failed to submit claims to service bus. MemberId={requestModel.MemberId}, ClaimIds={string.Join(",", requestModel.Ids)}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Sending claims to service bus topic for Approval
        /// </summary>
        /// <remarks>Sending claims to the service bus topic for Async Approval based on the provided model.</remarks>
        /// <param name="model">Request model for claim Approval</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the claim Approval.
        /// </returns>
        /// <response code="204">Success, Submit claim Approved</response>
        /// <response code="400">Validation error. See response for details</response>
        /// <response code="401">Authentication missing</response>
        /// <response code="403">Forbidden - caller not authorized</response>
        [HttpPost]
        public async Task<IActionResult> SubmitClaimsForApproval([FromBody] IdsWithUserInfo model)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Submitting claims for approval. MemberId={MemberId}, ClaimIds={ClaimIds}",
                    nameof(ClaimController),
                    nameof(SubmitClaimsForApproval),
                    model.MemberId, string.Join(",", model.Ids));

                await _claimService.SubmitClaimsToServiceBusTopicAsync(model);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(ClaimController)}.{nameof(SubmitClaimsForApproval)} -Error submitting claims to service bus topic: memberId={model.MemberId}, claimIds={string.Join(",", model.Ids)}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves claim flag reasons for the specified account.
        /// </summary>
        /// <remarks>
        /// Returns system-level and account-specific claim flag reasons that are active
        /// and available for use by the Claim Flagging UI.
        /// </remarks>
        /// <param name="accountInfoId">The account identifier.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the list of claim flag reasons.
        /// </returns>
        /// <response code="200">Success</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Authentication missing</response>
        /// <response code="403">Forbidden - caller not authorized</response>

        [HttpGet]
        public async Task<IActionResult> GetClaimFlagReasons([FromQuery] int accountInfoId)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim flag reasons. AccountInfoId={AccountInfoId}",
                    nameof(ClaimController),
                    nameof(GetClaimFlagReasons),
                    accountInfoId);

                if (accountInfoId <= 0)
                {
                    return BadRequest("accountInfoId is required and must be greater than zero.");
                }

                var reasons = await _claimService.GetClaimFlagReasonsAsync(accountInfoId);
                return Ok(reasons);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimFlagReasons)} - Error retrieving claim flag reasons: accountInfoId={accountInfoId}. Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the 'Other' Billing Provider/Organisation information stored for a claim.
        /// </summary>
        /// <param name="claimId">Unique identifier of the claim.</param>
        /// <returns>Billing provider details if present.</returns>
        [HttpGet]
        [ProducesResponseType(statusCode: 200, type: typeof(ClaimBillingProviderOtherDto))]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 401)]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 500)]
        public async Task<IActionResult> GetBillingProviderDetails([FromQuery] int claimId)
        {
            try
            {
                _logger.LogInformation(
                    "{Controller}.{Action} - Getting claim billing provider details. ClaimId={ClaimId}",
                    nameof(ClaimController),
                    nameof(GetBillingProviderDetails),
                    claimId);

                if (claimId <= 0)
                {
                    return BadRequest("claimId is required and must be greater than zero.");
                }

                var billingProvider = await _claimService.GetBillingProviderDetailsIdAsync(claimId);

                if (billingProvider == null)
                {
                    return NotFound();
                }

                return Ok(billingProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetBillingProviderDetails)} - Error retrieving claim billing provider details: claimId={claimId}. Error: {ex.Message}");
                 return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignClaimsRequest request)
        {
            try
            {

                if (request.ClaimIds == null || request.ClaimIds.Length == 0)
                    return BadRequest("ClaimIds cannot be empty.");

                var result = await _claimService.AssignClaimsAsync(request.ClaimIds, request.AssigneeId, request.MemberId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error Assign claims: AssigneeId={request.AssigneeId}. MemberId={request.MemberId} Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetLatestBillingProvidersAsync([FromBody] ClaimFilterGetModel requestModel)
        {
            try
            {
                _logger.LogInformation(
                  "{Controller}.{Action} - Getting claim flag reasons. AccountInfoId={AccountInfoId}",
                  nameof(ClaimController),
                  nameof(GetClaimFlagReasons),
                  requestModel.AccountInfoId);

                if (requestModel.AccountInfoId <= 0)
                {
                    return BadRequest("accountInfoId is required and must be greater than zero.");
                }

                var result = await _claimService.GetLatestBillingProvidersAsync(requestModel.AccountInfoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetClaimFlagReasons)} - Error retrieving claim flag reasons: accountInfoId={requestModel.AccountInfoId}. Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the list of states available in the system.
        /// </summary>
        /// <remarks>
        /// Fetches all active states from the State lookup table and returns them 
        /// as a list of <see cref="StateDto"/> objects. Only states that are not 
        /// soft-deleted (where DateDeleted is null) are included in the response.
        /// </remarks>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a collection of <see cref="StateDto"/>.
        /// </returns>
        /// <response code="200">Success - Returns the list of states.</response>
        /// <response code="400">Bad request - An error occurred while retrieving the state information.</response>
        /// <response code="401">Authentication missing or invalid.</response>
        /// <response code="403">Forbidden - Caller is not authorized to access this resource.</response>
        [HttpGet]
        public async Task<IActionResult> GetStateInformation()
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Getting State Information.", nameof(ClaimController), nameof(GetStateInformation));

                var result = await _claimService.GetStatesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetStateInformation)} - Error retrieving state information. Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// Retrieves all external codes.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches external codes of type <see cref="ExternalCodeType.ClaimStatusCode"/> 
        /// from the underlying service. Data may be served from cache for performance optimization.
        /// </remarks>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing a list of 
        /// <see cref="ExternalCodeResponseModel"/> on success.
        /// </returns>
        /// <response code="200">External codes retrieved successfully.</response>
        /// <response code="400">An error occurred while processing the request.</response>
        [HttpGet]
        public async Task<IActionResult> GetAllExternalCodes()
        {
            _logger.LogInformation("{Controller}.{Action} - GetAllExternalCodes called.",
                nameof(ClaimController),
                nameof(GetAllExternalCodes));

            try
            {
                var result = await _claimService.GetAllExternalCodes();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimController)}.{nameof(GetAllExternalCodes)} -GetAllExternalCodes failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}