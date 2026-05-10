using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Provider;
using BillingService.Domain.Models;

using BillingService.Domain.Models.Clients;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Services.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Newtonsoft.Json;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ClientFunderModel = BillingService.Domain.Models.Clients.ClientFunderModel;

namespace BillingService.Domain.Services.Client
{
    public class ClientService : BaseService, IClientService
    {
        private readonly IProviderService _providerService;
        private readonly IRethinkMasterDataMicroServices _rethinkMicroservicesRepository;

        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        // Create a static cache key so these values can be shared across instances of the service.
        // These are not assiciated with any Account all these are Generic or MasterData
        private readonly string cacheKey = "Global_DiagnosisCodes_For_All";
        private static readonly TimeSpan cacheExpiration = TimeSpan.FromHours(24);

        public ClientService(
                    IProviderService providerService,
                    IConfiguration configuration,
                    IRethinkMasterDataMicroServices rethinkMicroservicesRepository,
                    ICacheService cacheService
            )
        {
            _rethinkMicroservicesRepository = rethinkMicroservicesRepository;
            _providerService = providerService;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        public async Task<List<ClientOptionModel>> GetClientsListForClaimAsync(int accountId, int memberId)
        {
            var result = await _rethinkMicroservicesRepository.GetChildProfile(accountId);

            if (result != null)
            {
                return result.Select(x => new ClientOptionModel
                {
                    Id = x.Id,
                    Name = $"{x.LastName}, {x.FirstName + " - " + x.Id}"
                }).ToList();
            }

            return new List<ClientOptionModel>();
        }

        public async Task<List<ClientDiagnosis>> SearchDiagnosis(string searchTerm, int? diagnosisTypeId,
            int accountInfoId, int? excludeDiagnosisTypeId = null)
        {

            var result = await _cacheService.GetOrSetCacheAsync(
                cacheKey,
                async () => await _rethinkMicroservicesRepository.GetAllDiagnosisAsync(accountInfoId),
                cacheExpiration);

            var selectQuery = result.Where(x => x.diagnosisCode != null);

            if (!searchTerm.Any(char.IsDigit))
                selectQuery = selectQuery.Where(x => x.name.ToLower().StartsWith(searchTerm.ToLower(), StringComparison.OrdinalIgnoreCase));

            if (searchTerm.Any(char.IsDigit))
            {
                selectQuery = selectQuery.Where(x => x.diagnosisCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                     //descending here because true returns 1 and false - 0
                    //.OrderByDescending(x => x.diagnosisCode.Contains(searchTerm))
                    //.ThenBy(x => x.diagnosisCode)
                    //.ThenBy(x => x.name).ToList();
            }
            else
            {
                selectQuery = selectQuery
                    //descending here because true returns 1 and false - 0
                    .OrderByDescending(x => x.name.StartsWith(searchTerm))
                    .ThenBy(x => x.name)
                    .ThenBy(x => x.diagnosisCode).ToList();
            }

            var data = selectQuery
                 .Take(50)
                 .Select(x => new ClientDiagnosis
                 {
                     DiagnosisId = x.id,
                     DiagnosisLUDescription = x.name,
                     DiagnosisLUCode = x.diagnosisCode,
                     AccountInfoId = x.accountInfoId,
                     IsCustom = x.accountInfoId > 0
                 }).ToList();

            return data;
        }

        public async Task<int> GetClientFacilityIdAsync(int clientId, int accountInfoId)
        {
            var result = await _rethinkMicroservicesRepository.GetChildProfileFacility(accountInfoId, clientId);
            return result != null ? result.providerLocationId : 0;
        }

        [Obsolete]
        public async Task<List<Models.Clients.ClientFunderModel>> ExistingGetClientFundersSmallAsync(int clientId, int accountInfoId, bool loadAllFunderTypes = false)
        {
            //Existing GetClientFundersSmallAsync 
            //GetClientFundersSmallAsync method name is  Now updated to GetClientFundersAsync
            List<ClientFunderModel> funderObj = new List<ClientFunderModel>();

            var fundersDetails = await _rethinkMicroservicesRepository.GetChildProfileFunderMappings(accountInfoId, clientId);

            if (fundersDetails != null)
            {
                var activeFunderServiceLine = fundersDetails.data.Where(x => x.metaData.deletedOn == null && x.insuranceType == ResponsibilitySequenceType.Primary).DistinctBy(x => x.funderId).ToList();

                foreach (var item in activeFunderServiceLine)
                {
                    //get funder information
                    var funderDetails = await _rethinkMicroservicesRepository.GetFunder(accountInfoId, item.funderId);
                    // check if funder type is private pay or self pay then skip it
                    if (funderDetails != null && (funderDetails.funderTypeId == (int)FunderType.PrivatePay || funderDetails.funderTypeId == (int)FunderType.SelfPay))
                    {
                        continue;
                    }

                    item.InsuranceContact = await _rethinkMicroservicesRepository.GetInsuranceContactEntity(accountInfoId, clientId, item.childProfileInsuranceContactId);
                    if (item.InsuranceContact != null)
                    {
                        item.InsuranceContact.InsuranceContactsType = await _rethinkMicroservicesRepository.GetInsuranceContactsType(accountInfoId, clientId, item.childProfileInsuranceContactId);
                    }

                    ClientFunderModel funder = new ClientFunderModel();
                    funder.Id = item.id;
                    funder.FunderId = item.funderId;

                    funder.ServiceLines = new List<FunderServiceLineModel>();

                    if (funderDetails != null)
                    {
                        funder.FunderName = item.InsuranceContact != null && item.InsuranceContact.InsuranceContactsType != null ?
                                                $"{funderDetails.funderName} - {item.InsuranceContact.InsuranceContactsType.insurancePolicyNumber + " - " + funder.Id}" :
                                                $"{funderDetails.funderName + " - " + funder.Id}";
                        funder.FunderType = (FunderType)funderDetails.funderTypeId;
                        funder.ReferringProviderRequiredOnClaim = funderDetails.referringProviderRequiredOnClaim;
                        funder.BillingProviderOptionId = funderDetails.billingProviderOptionId;
                    }

                    funderObj.Add(funder);
                }
            }
            return funderObj;
        }
        public async Task<List<ClientFunderModel>> GetClientFundersAsync( int clientId, int accountInfoId,  bool loadAllFunderTypes = false)
        {
            var fundersDetails = await _rethinkMicroservicesRepository
                .GetChildProfileFunderMappings(accountInfoId, clientId);
            if (fundersDetails?.data == null)
                return new List<ClientFunderModel>();
            var activeFunders = fundersDetails.data
                .Where(x => x.metaData.deletedOn == null &&
                            x.insuranceType == ResponsibilitySequenceType.Primary)
                .DistinctBy(x => x.funderId)
                .ToList();
            if (!activeFunders.Any())
                return new List<ClientFunderModel>();
            // ---------------- PARALLEL FETCH ----------------
            var funderTasks = activeFunders
                .Select(x => _rethinkMicroservicesRepository.GetFunder(accountInfoId, x.funderId))
                .ToList();
            var contactTasks = activeFunders
                .Select(x => _rethinkMicroservicesRepository
                    .GetInsuranceContactEntity(accountInfoId, clientId, x.childProfileInsuranceContactId))
                .ToList();
            var funderResults = await Task.WhenAll(funderTasks);
            var contactResults = await Task.WhenAll(contactTasks);
            
            var contactTypeTasks = activeFunders
                .Select(x => _rethinkMicroservicesRepository
                    .GetInsuranceContactsType(accountInfoId, clientId, x.childProfileInsuranceContactId))
                .ToList();
            var contactTypeResults = await Task.WhenAll(contactTypeTasks);
            // ---------------- BUILDING RESULT ----------------
            var result = new List<ClientFunderModel>();
            for (int i = 0; i < activeFunders.Count; i++)
            {
                var item = activeFunders[i];
                var funderDetails = funderResults[i];
                var contact = contactResults[i];
                var contactType = contactTypeResults[i];
                if (funderDetails == null)
                    continue;
                if (funderDetails.funderTypeId == (int)FunderType.PrivatePay ||
                     funderDetails.funderTypeId == (int)FunderType.SelfPay)
                    continue;
                var funder = new ClientFunderModel
                {
                    Id = item.id,
                    FunderId = item.funderId,
                    ServiceLines = new List<FunderServiceLineModel>(),
                    FunderType = (FunderType)funderDetails.funderTypeId,
                    ReferringProviderRequiredOnClaim = funderDetails.referringProviderRequiredOnClaim,
                    BillingProviderOptionId = funderDetails.billingProviderOptionId,
                    FunderName =
                        contactType != null
                            ? $"{funderDetails.funderName} - {contactType.insurancePolicyNumber} - {item.id}"
                            : $"{funderDetails.funderName} - {item.id}"                  
                };
                result.Add(funder);
            }
            return result;
        }
        public async Task<List<FunderServiceLineModel>> GetFunderServiceLinesAsync(int id, int funderId, int accountInfoId, int clientId)
        {
            List<FunderServiceLineModel> funderServiceLineModels = new List<FunderServiceLineModel>();
            var serviceLineMappings = await _rethinkMicroservicesRepository.GetServiceLineMappingsByFunderId(accountInfoId, clientId, id);

            if (serviceLineMappings != null)
            {
                foreach (var serviceLineMapping in serviceLineMappings.Where(x => x.metaData.deletedOn == null))
                {
                    FunderServiceLineModel funderServiceLineModel = new FunderServiceLineModel();
                    funderServiceLineModel.MappingId = serviceLineMapping.id;
                    funderServiceLineModel.ServiceId = serviceLineMapping.serviceId;
                    funderServiceLineModel.Sequence = serviceLineMapping.responsibilitySequence.ToString();
                    funderServiceLineModel.BillingProviderOptionId = 0; // serviceFunderList.Where(x => x.providerServiceId == serviceLineMapping.serviceId).FirstOrDefault().billingProviderOptionId;

                    var serviceLineInfo = await _rethinkMicroservicesRepository.GetServiceLine(accountInfoId, serviceLineMapping.serviceId);
                    funderServiceLineModel.Name = serviceLineInfo.name;

                    funderServiceLineModels.Add(funderServiceLineModel);
                }
            }
            return funderServiceLineModels;
        }

        public async Task<ClientFunderResponsiblePartiesModel> GetClientFunderResponsiblePartiesAsync(int memberId, int accountId, int clientId, int clientFunderId)
        {
            var result = new ClientFunderResponsiblePartiesModel();
            var resultClientFunderMapping = await _rethinkMicroservicesRepository.GetChildProfileFunderMappings(accountId, clientId);
            var person = await _rethinkMicroservicesRepository.GetChildProfile(accountId, clientId);

            if (resultClientFunderMapping != null)
            {
                var funderMapping = resultClientFunderMapping.data.Where(x => x.funderId == clientFunderId).Select(x => x.childProfileInsuranceContactId).FirstOrDefault();

                var resInsuranceContacts = await _rethinkMicroservicesRepository.GetInsuranceContactEntity(accountId, clientId, funderMapping);
                var insuredContacts = new ClientContact();

                if (person != null)
                {
                    insuredContacts.Id = resInsuranceContacts.Id;
                    insuredContacts.FirstName = resInsuranceContacts?.Name?.firstName;
                    insuredContacts.MiddleName = resInsuranceContacts?.Name?.middleName;
                    insuredContacts.LastName = resInsuranceContacts?.Name?.lastName;
                    //insuredContacts.PersonId = person.id;
                    insuredContacts.RelationshipToInsured = resInsuranceContacts.RelationshipToInsured;
                }
                result.InsuranceContact = insuredContacts;
            }
            else
            {
                result.InsuranceContact = new ClientContact();
            }


            var demographics = new ClientDemographics()
            {
                Id = person.id,
                FirstName = person?.name?.firstName,
                LastName = person?.name?.lastName
            };
            result.ClientDemographics = demographics;
            return result;
        }

        [Obsolete]
        public async Task<ClaimCreateInfoModel> ExistingGetClaimCreateInfoAsync(ClaimCreateInfoGetModel model, int accountInfoId)
        {
            //Existing  GetClaimCreateInfoAsync 
            var billingCodes = new List<ClientAuthorizationBillingCodeSmall>();

            var renderingProviders = await _rethinkMicroservicesRepository.GetRenderingProvidersAsync(accountInfoId, true);

            var billingCodeResult = await _rethinkMicroservicesRepository.GetBillingCodeList(accountInfoId);

            if (billingCodeResult != null)
            {
                var billingCodesModel = billingCodeResult.data.Where(x => x.providerServiceId == model.ServiceId && x.funderId == model.FunderId && x.metaData.deletedOn == null).ToList();

                billingCodeResult.data = billingCodesModel;

                foreach (var item in billingCodeResult.data)
                {
                    ClientAuthorizationBillingCodeSmall billingCodeModel = new ClientAuthorizationBillingCodeSmall();
                    billingCodeModel.Inactive = item.inactive;
                    billingCodeModel.FunderId = item.funderId;
                    billingCodeModel.BillingCodeDescription = item.description;
                    billingCodeModel.BillingCodeName = item.billingCode;
                    billingCodeModel.BillingCodeId = item.id;
                    billingCodeModel.BillingCodeName2 = item.billingCode2;
                    billingCodeModel.FrequencyTypeId = item.frequencyTypeId;
                    billingCodeModel.NoAuthRequired = item.noAuthRequired;
                    billingCodeModel.ProviderServiceId = item.providerServiceId;
                    billingCodeModel.ServiceLineId = item.serviceId;
                    billingCodeModel.UnitTypeId = item.unitTypeId;
                    billingCodeModel.UnitTypeId2 = item.unitTypeId2;
                    billingCodeModel.Rate = item.rate;
                    billingCodeModel.Rate2 = item.rate2;

                    var serviceName = await _rethinkMicroservicesRepository.GetProviderService(accountInfoId, item.serviceId);
                    billingCodeModel.ServiceName = serviceName != null ? serviceName.name : "Service Name";

                    if (!billingCodeModel.Inactive.GetValueOrDefault())
                    {
                        billingCodes.Add(billingCodeModel);
                    }
                }
            }



            var locations = await _providerService.GetProviderLocationList(accountInfoId, settings);


            //******************************Referring provider list start***********************************************************************************

            var referringProviderList = await _rethinkMicroservicesRepository.GetReferringProvidersByClientId(model.ClientId, accountInfoId);

            //******************************Referring provider list end***********************************************************************************

            //UnComment it After the referring providers API is ready : Saurabh

            /*var referringProviders = await _childProfileReferringProvidersRepository
                .Query()
                .Include(x => x.ReferringProvider)
                .Where(x => x.DateDeleted == null && x.ChildProfileId == model.ClientId)
                .Select(x => new ClientReferringProviderForDropdownModel
                {
                    Id = x.Id,
                    ProviderName = x.ReferringProvider.FirstName != null ? x.ReferringProvider.FirstName + ' ' + x.ReferringProvider.LastName : x.ReferringProvider.LastName,
                    IsDefault = x.IsDefault,
                    IsActive = x.ReferringProvider.IsActive
                })
                .ToListAsync();*/


            return new ClaimCreateInfoModel
            {
                Locations = locations,
                BillingCodes = billingCodes,
                ReferringProviders = referringProviderList.Select(x => new ClientReferringProviderForDropdownModel
                {
                    Id = x.Id,
                    ProviderName = x.ProviderName,
                    FirstName = x.FirstName,
                    MiddleName = x.MiddleName,
                    LastName = x.LastName,
                    IsActive = x.IsActive,
                    IsDefault = x.IsDefault

                }).ToList(),
                RenderingProviders = renderingProviders,
            };
        }

        public async Task<ClaimCreateInfoModel> GetClaimCreateInfoAsync(ClaimCreateInfoGetModel model, int accountInfoId)
        {
            // ---------- PARALLEL CALLS ----------
            var renderingTask = _rethinkMicroservicesRepository
                .GetRenderingProvidersAsync(accountInfoId, true);

            var billingTask = _rethinkMicroservicesRepository
                .GetBillingCodeList(accountInfoId);

            var locationTask = _providerService
                .GetProviderLocationList(accountInfoId, settings);

            var referringTask = _rethinkMicroservicesRepository
                .GetReferringProvidersByClientId(model.ClientId, accountInfoId);

            await Task.WhenAll(
                  renderingTask,
                  billingTask,
                  locationTask,
                  referringTask
            );

            // ---------- RESULTS ----------
            var renderingProviders = await renderingTask;
            var billingCodeResult = await billingTask;
            var locations = await locationTask;
            var referringProviderList = await referringTask;
            
            //Service fetch code starts
            var filtered = billingCodeResult?.data?
                           .Where(x =>
                               x.providerServiceId == model.ServiceId &&
                               x.funderId == model.FunderId &&
                               x.metaData.deletedOn == null &&
                               !x.inactive)
                           .ToList() ?? new List<BillingCodes>();
                           
                                   var distinctServiceIds = filtered
                           .Select(x => x.serviceId)
                           .Distinct()
                           .ToList();

                                   var serviceTasks = distinctServiceIds
                           .Select(id => _rethinkMicroservicesRepository
                               .GetProviderService(accountInfoId, id))
                           .ToList();
                           
                                   var serviceResults = await Task.WhenAll(serviceTasks);
                           
                                   var serviceDict = serviceResults
                           .Where(s => s != null)
                           .GroupBy(s => s.id)
                           .ToDictionary(g => g.Key, g => g.First().name);


            var billingCodes = filtered
                .Select(item => new ClientAuthorizationBillingCodeSmall
               {
                    Inactive = item.inactive,
                    FunderId = item.funderId,
                    BillingCodeDescription = item.description,
                    BillingCodeName = item.billingCode,
                    BillingCodeId = item.id,
                    BillingCodeName2 = item.billingCode2,
                    FrequencyTypeId = item.frequencyTypeId,
                    NoAuthRequired = item.noAuthRequired,
                    ProviderServiceId = item.providerServiceId,
                    ServiceLineId = item.serviceId,
                    UnitTypeId = item.unitTypeId,
                    UnitTypeId2 = item.unitTypeId2,
                    Rate = item.rate,
                    Rate2 = item.rate2,
                    ServiceName = serviceDict.TryGetValue(item.serviceId, out var name)
                                    ? name
                                    : "Service Name"
                }).ToList()  ?? new List<ClientAuthorizationBillingCodeSmall>();

            // ---------- RETURN ----------
            return new ClaimCreateInfoModel
            {
                Locations = locations,
                BillingCodes = billingCodes,
                RenderingProviders = renderingProviders,
                ReferringProviders = referringProviderList
                    .Select(x => new ClientReferringProviderForDropdownModel
                    {
                        Id = x.Id,
                        ProviderName = x.ProviderName,
                        FirstName = x.FirstName,
                        MiddleName = x.MiddleName,
                        LastName = x.LastName,
                        IsActive = x.IsActive,
                        IsDefault = x.IsDefault
                    })
                    .ToList()
            };
        }

        public async Task<List<DiagnosisCodeForClaimWithoutAuthModel>> GetDiagnosisForClaimWithoutAuthAsync(int clientId, int serviceLine, int accountInfoId)
        {
            var resDignosisCodes = await _rethinkMicroservicesRepository.GetClientDiagnosisByServiceId(accountInfoId, clientId, serviceLine);

            var result = resDignosisCodes.data.Where(x => x.endDate == null || x.endDate > DateTime.Now)
                .Select(x => new DiagnosisCodeForClaimWithoutAuthModel
                {
                    DiagnosisId = (int)x.diagnosisId,
                    DiagnosisCode = x.diagnosis.diagnosisCode,
                    DiagnosisDescription = x.diagnosis.description,
                    DiagnosisFullDescription = x.diagnosis.diagnosisCode + " - " + x.diagnosis.description,
                    Order = x.order,
                    IncludeOnClaims = true,
                    //StartDate=x.startDate,
                    EndDate = x.endDate
                })
                .ToList();

            return result;
        }

        public async Task<List<BaseNameOption>> GetClientAuthorizationsForClaimAsync(int childProfileId, int funderId, int clientFunderServiceLineId, int accountInfoId)
        {
            var resAuthorizations = await _rethinkMicroservicesRepository.GetClientAuthorizationsByClientId(accountInfoId, childProfileId);

            if (resAuthorizations != null)
            {
                var authorizations = resAuthorizations.data.Where(x => x.funderId == funderId && x.providerServiceId == clientFunderServiceLineId).ToList();

                var childList = authorizations.Select(x => new BaseNameOption
                {
                    Id = x.id,
                    Name = $"{x.authorizationNumber} ({x.startDate.ToShortDateString()} - {x.endDate.ToShortDateString()})"
                }).ToList();

                return childList;
            }
            return new List<BaseNameOption>();
        }

        [Obsolete]
        public async Task<ClientAuthorizationModel> ExistingGetClientAuthorization(int authorizationId, int childProfileId,
             int memberId, int accountInfoId, string LocaleString)
        {
            //Existing GetClientAuthorization 
            var result = new ClientAuthorizationModel();

            var authorizationResponse = await _rethinkMicroservicesRepository.GetChildProfileAuthorizationByClientId(accountInfoId, childProfileId, authorizationId);

            if (authorizationResponse != null)
            {
                var funderMappings = await _rethinkMicroservicesRepository.GetChildProfileFunderMappings(accountInfoId, childProfileId);
                var funderMapping = funderMappings.data.FirstOrDefault(x => x.funderId == authorizationResponse.funderId);
                authorizationResponse.Funder = await _rethinkMicroservicesRepository.GetFunder(accountInfoId, authorizationResponse.funderId);
                var mainLocation = await _rethinkMicroservicesRepository.GetMainLocation(accountInfoId);
                var providerServiceProviderLocationId = await GetClientFacilityIdAsync(childProfileId, accountInfoId);
                result = new ClientAuthorizationModel()
                {
                    Id = authorizationResponse.id,
                    /*IsActive = authorizationResponse.endDate <= EstDateTime.Date && authorizationResponse.endDate.Date >= EstDateTime.Date
                        && (authorizationResponse.InactiveDate == null || x.InactiveDate.Value.Date > EstDateTime.Date),*/
                    AuthorizationNumber = authorizationResponse.authorizationNumber,
                    AuthorizationSubmissionTypeId = authorizationResponse.authorizationSubmissionTypeId,
                    FunderId = authorizationResponse.funderId,
                    /*FunderAppointmentExceedingAuthorizationAlertId = x.Funder.AppointmentExceedingAuthorizationAlertId,*/
                    RenderingProviderId = authorizationResponse.renderingProviderStaffId,
                    RenderingProviderTypeId = authorizationResponse.authorizationRenderingProviderTypeId,
                    StartDate = authorizationResponse.startDate,
                    EndDate = authorizationResponse.endDate,
                    /*InactiveDate = x.InactiveDate,*/
                    /*DiactivatedById = x.DiactivatedById,*/
                    /*ReferringProviderIsActive = x.ChildProfileReferringProvider.ReferringProvider.IsActive,*/
                    ReferringProviderId = authorizationResponse.childProfileReferringProviderId,
                    BillingProviderId = providerServiceProviderLocationId,
                    //Saurabh: Commenting below line as client setup service Provider Location is on priority confirmed on the discussion with QA.
                    //ServiceProviderId = authorizationResponse.Funder.providerLocationId,
                    ServiceProviderId = providerServiceProviderLocationId,
                    ServiceLineId = authorizationResponse.providerServiceId,
                    AuthorizationDistributionTypeId = authorizationResponse.authorizationDistributionTypeId,
                    TotalNumberOfUnits = authorizationResponse.totalNumberOfUnits,
                    ChildProfileFunderServiceLineMappingId = authorizationResponse.childProfileFunderServiceLineMappingId.FirstOrDefault(),
                    ChildProfileFunderMappingId = funderMapping.id,
                    //ShowAuthorizationByTypeId = x.ShowAuthorizationByTypeId,
                    /*AccountOrganizationName = x.ChildProfile.AccountInfo.AccountOrganizationName*/
                };

                if (result != null)
                {
                    var renderingProviders = await _rethinkMicroservicesRepository.GetAllRenderingProvidersAsync(accountInfoId);
                    var renderingProvider = renderingProviders.data.FirstOrDefault(x => x.memberId == result.RenderingProviderId);
                    result.RenderingProviderTypeId = renderingProvider.id;

                    result.DiagnosisCodes = new List<ClientAuthorizationDiagnosisCodeModel>();
                    if (authorizationResponse.childProfileDiagnosisId > 0)
                    {
                        var diagnosisObj = new ClientAuthorizationDiagnosisCodeModel();

                        var diaCodes = await _rethinkMicroservicesRepository.GetChildProfileAuthorizationDiagnosisCodesAsync(accountInfoId, childProfileId, authorizationResponse.childProfileDiagnosisId, authorizationResponse.id);

                        result.DiagnosisCodes = diaCodes.Select(x => new ClientAuthorizationDiagnosisCodeModel
                        {
                            Id = x.id,
                            DiagnosisId = x.diagnosisId,
                            DiagnosisFullDescription = x.Diagnosis.diagnosisCode + "-" + x.Diagnosis.description,
                            Description = x.Diagnosis.description,
                            IncludeOnClaims = true,
                            Order = 1,
                            DiagnosisCode = x.Diagnosis.diagnosisCode
                        }
                        ).ToList();
                    }

                    result.BillingCodes = new List<ClientAuthorizationBillingCodeModel>();
                    var associatedBillingCodes = await _rethinkMicroservicesRepository.GetClientAuthBillingCodesByAuthId(accountInfoId, childProfileId, authorizationId);
                    foreach (var item in associatedBillingCodes)
                    {
                        var billingCodeToCheck = await _rethinkMicroservicesRepository.GetProviderBillingCode(accountInfoId, item.BillingCodeId);
                        if (!billingCodeToCheck.inactive.GetValueOrDefault())
                        {
                            result.BillingCodes.Add(item);
                        }

                    }
                }
            }

            //since some diagnosis codes are inactive so to avaoid further confusion code to reorder diagnosis codes
            var order = 1;

            var ActiveCodes = result.DiagnosisCodes.OrderBy(x => x.Order).ToList();
            ActiveCodes.RemoveAll(x => !x.IncludeOnClaims);
            foreach (var item in ActiveCodes)
            {
                if (item.Order != order)
                    item.Order = order;
                order++;
            }

            var childProfileFunder = await _rethinkMicroservicesRepository.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, result.ChildProfileFunderMappingId ?? 0);
            /*var childProfileFunder = _childProfileFunderMappingRepository.Query()
                .FirstOrDefault(x => x.Id == result.ChildProfileFunderMappingId && x.DateDeleted == null);*/
            //#69171 - Authorization. Auth in 'Inactive' status for active Funder with 'End date' = today's date
            var validFunderEndDate = childProfileFunder.endDate != null ? childProfileFunder.endDate.Value.Date >= EstDateTime.Date : true;
            var validFunderStartDate = childProfileFunder.startDate != null && childProfileFunder.startDate.Value.Date <= EstDateTime.Date;
            var isFunderActive = childProfileFunder != null ? (validFunderEndDate && validFunderStartDate) : true;
            var validInactiveDate = result.InactiveDate == null || result.InactiveDate.Value.Date > EstDateTime.Date;
            result.IsActive = result.IsActive && isFunderActive && validInactiveDate;
            result.IsInactiveDateValid = validInactiveDate;
            result.IsFunderValid = isFunderActive;
            result.IsStartDateValid = result.StartDate <= EstDateTime;
            result.IsEndDateValid = result.EndDate.Date >= EstDateTime.Date;

            result.ChildProfileFunderMappingIsActive = isFunderActive;

            return result;
        }


        public async Task<ClientAuthorizationModel> GetClientAuthorization(int authorizationId, int childProfileId,
             int memberId, int accountInfoId, string LocaleString)
        {
            // ---------- STEP 1 — Required first ----------
            var authorizationResponse =
                await _rethinkMicroservicesRepository
                .GetChildProfileAuthorizationByClientId(
                    accountInfoId, childProfileId, authorizationId);

            if (authorizationResponse == null)
                return new ClientAuthorizationModel();

            // ---------- STEP 2 — PARALLEL CALLS ----------
            var funderMappingsTask =
                _rethinkMicroservicesRepository
                .GetChildProfileFunderMappings(accountInfoId, childProfileId);

            var funderTask =
                _rethinkMicroservicesRepository
                .GetFunder(accountInfoId, authorizationResponse.funderId);

            var mainLocationTask =
                _rethinkMicroservicesRepository
                .GetMainLocation(accountInfoId);

            var facilityTask =
                GetClientFacilityIdAsync(childProfileId, accountInfoId);

            var renderingProvidersTask =
                _rethinkMicroservicesRepository
                .GetAllRenderingProvidersAsync(accountInfoId);

            var billingCodesTask =
                _rethinkMicroservicesRepository
                .GetClientAuthBillingCodesByAuthId(
                    accountInfoId, childProfileId, authorizationId);

            var diagnosisTask = authorizationResponse.childProfileDiagnosisId > 0 ?
                                _rethinkMicroservicesRepository.GetChildProfileAuthorizationDiagnosisCodesAsync(
                                accountInfoId,
                                childProfileId,
                                authorizationResponse.childProfileDiagnosisId,
                                authorizationResponse.id) : 
                                Task.FromResult(new List<Rethink.Services.Common.Models.ChildProfileAuthorizationDiagnosisCode>());



            await Task.WhenAll(
                funderMappingsTask,
                funderTask,
                mainLocationTask,
                facilityTask,
                renderingProvidersTask,
                billingCodesTask,
                diagnosisTask
            );

            // ---------- STEP 3 — Resolve ----------
            var funderMappings = await funderMappingsTask;
            var funderMapping = funderMappings.data
                .FirstOrDefault(x => x.funderId == authorizationResponse.funderId);

            var providerServiceProviderLocationId = await facilityTask;

            var renderingProviders = await renderingProvidersTask;
            var renderingProvider = renderingProviders.data
                .FirstOrDefault(x => x.memberId ==
                                     authorizationResponse.renderingProviderStaffId);

            // ---------- STEP 4 — Build Result ----------
            var result = new ClientAuthorizationModel
            {
                Id = authorizationResponse.id,
                AuthorizationNumber = authorizationResponse.authorizationNumber,
                AuthorizationSubmissionTypeId =
                    authorizationResponse.authorizationSubmissionTypeId,
                FunderId = authorizationResponse.funderId,
                RenderingProviderId =
                    authorizationResponse.renderingProviderStaffId,
                RenderingProviderTypeId = renderingProvider?.id,
                StartDate = authorizationResponse.startDate,
                EndDate = authorizationResponse.endDate,
                ReferringProviderId =
                    authorizationResponse.childProfileReferringProviderId,
                BillingProviderId = providerServiceProviderLocationId,
                ServiceProviderId = providerServiceProviderLocationId,
                ServiceLineId = authorizationResponse.providerServiceId,
                AuthorizationDistributionTypeId =
                    authorizationResponse.authorizationDistributionTypeId,
                TotalNumberOfUnits =
                    authorizationResponse.totalNumberOfUnits,
                ChildProfileFunderServiceLineMappingId =
                    authorizationResponse.childProfileFunderServiceLineMappingId
                        .FirstOrDefault(),
                ChildProfileFunderMappingId = funderMapping?.id
            };

            // ---------- STEP 5 — Diagnosis Mapping ----------
            var diaCodes = await diagnosisTask;
            result.DiagnosisCodes = diaCodes.Select(x =>
                new ClientAuthorizationDiagnosisCodeModel
                {
                    Id = x.id,
                    DiagnosisId = x.diagnosisId,
                    DiagnosisFullDescription =
                        x.Diagnosis.diagnosisCode + "-" +
                        x.Diagnosis.description,
                    Description = x.Diagnosis.description,
                    IncludeOnClaims = true,
                    Order = 1,
                    DiagnosisCode = x.Diagnosis.diagnosisCode
                }).ToList();

            // ---------- STEP 6 — BILLING CODE BULK FILTER ----------
            var associatedBillingCodes = await billingCodesTask;

            var allBillingCodes =
                await _rethinkMicroservicesRepository
                    .GetBillingCodeList(accountInfoId);

            var activeBillingIds = allBillingCodes.data
                .Where(x => !(x.inactive))
                .Select(x => x.id)
                .ToHashSet();

            result.BillingCodes = associatedBillingCodes
                .Where(x => activeBillingIds.Contains(x.BillingCodeId))
                .ToList();

            // ---------- STEP 7 — reorder diagnosis ----------
            int order = 1;
            foreach (var item in result.DiagnosisCodes
                         .Where(x => x.IncludeOnClaims)
                         .OrderBy(x => x.Order))
            {
                item.Order = order++;
            }

            // ---------- STEP 8 — funder validity ----------
            var childProfileFunder =
                await _rethinkMicroservicesRepository
                .GetChildProfileFunderMappingByMappingId(
                    accountInfoId,
                    childProfileId,
                    result.ChildProfileFunderMappingId ?? 0);

            var validFunderEndDate =
                childProfileFunder?.endDate?.Date >= EstDateTime.Date
                || childProfileFunder?.endDate == null;

            var validFunderStartDate =
                childProfileFunder?.startDate?.Date <= EstDateTime.Date;

            var isFunderActive =
                childProfileFunder == null
                || (validFunderEndDate && validFunderStartDate);

            var validInactiveDate =
                result.InactiveDate == null
                || result.InactiveDate.Value.Date > EstDateTime.Date;

            result.IsActive =
                result.IsActive &&
                isFunderActive &&
                validInactiveDate;

            result.IsInactiveDateValid = validInactiveDate;
            result.IsFunderValid = isFunderActive;
            result.IsStartDateValid = result.StartDate <= EstDateTime;
            result.IsEndDateValid = result.EndDate.Date >= EstDateTime.Date;
            result.ChildProfileFunderMappingIsActive = isFunderActive;

            return result;
        }
        private string GetFullName(string n1 = null, string n2 = null, string n3 = null)
        {
            return string.Join(' ', new[] { n1, n2, n3 }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}
