using BillingService.Domain.Models.ClientMicroServicesModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Mapper;
using Rethink.Services.Domain.Services.RethinkServices;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.RethinkMasterDataMicroservices
{

    public class RethinkMasterDataMicroServices : BaseService, IRethinkMasterDataMicroServices
    {
        #region "Other APIs"
        private readonly IConfiguration _configuration;
        private readonly IRethinkMasterDataSessionCache? _sessionCache;
        private readonly IRethinkBillingRequestContext? _requestContext;
        private readonly ILogger<RethinkMasterDataMicroServices>? _logger;
        private readonly HttpClient _httpAccountsClient;
        private readonly HttpClient _httpCurriculumClient;
        private readonly HttpClient _httpDemographicsClient;
        private readonly HttpClient _httpHealthPlansClient;
        private readonly HttpClient _httpHealthInsuranceClient;
        private readonly HttpClient _httpMedicalRecordsClient;
        private readonly HttpClient _httpPracticeOperationsClient;
        private readonly HttpClient _httpAppointmentClient;
        public RethinkMasterDataMicroServices(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IRethinkMasterDataSessionCache? sessionCache = null,
            IRethinkBillingRequestContext? requestContext = null,
            ILogger<RethinkMasterDataMicroServices>? logger = null)
        {
            _configuration = configuration;
            _sessionCache = sessionCache;
            _requestContext = requestContext;
            _logger = logger;
            _httpAccountsClient = httpClientFactory.CreateClient("accountsClient");
            _httpCurriculumClient = httpClientFactory.CreateClient("curriculumClient");
            _httpDemographicsClient = httpClientFactory.CreateClient("demographicsClient");
            _httpHealthPlansClient = httpClientFactory.CreateClient("healthPlansClient");
            _httpHealthInsuranceClient = httpClientFactory.CreateClient("healthInsuranceClient");
            _httpMedicalRecordsClient = httpClientFactory.CreateClient("medicalRecordsClient");
            _httpPracticeOperationsClient = httpClientFactory.CreateClient("praticeOperationsClient");
            _httpAppointmentClient = httpClientFactory.CreateClient("appointmentClient");
        }

        public async Task<string> GetProviderLocationName(int accountInfoId, int childProfileId)
        {
            var clientLocationCodes = await CallPracticeOperationsRequest<ProviderLocationModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/facility");

            if (clientLocationCodes != null)
            {
                var clientProviderLocations = await CallPracticeOperationsRequest<ProviderLocations>("/accounts/" + accountInfoId + "/providerLocations/" + clientLocationCodes.providerLocationId);
                if (clientProviderLocations != null)
                {
                    return clientProviderLocations.name;
                }
            }
            return string.Empty;
        }
        public async Task<List<LocationCodesModel>> GetLocationCodes()
        {
            var result = await CallHealthInsuranceRequest<List<LocationCodesModel>>("/definitions/locationCodes");
            return result;
        }

        public async Task<PlacesOfServiceModel> GetPlaceOfService(int accountInfoId)
        {
            var result = await CallPracticeOperationsRequest<PlacesOfServiceModel>("/accounts/" + accountInfoId + "/placesOfService");
            return result;
        }

        public async Task<ClearingHouseModel> GetClearingHouseDetails()
        {
            var result = await CallHealthInsuranceRequest<ClearingHouseModel>("/definitions/clearingHouses");
            return result;
        }

        public async Task<RethinkClientDetails> GetClientDetails(int accountInfoId, int clientId)
        {
            var result = await CallPracticeOperationsRequest<RethinkClientDetails>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/details");
            return result;
        }

        public async Task<List<RethinkGuarantorDetails.ClientModel>> GetClientDetailsGuarantor(int accountInfoId)
        {
            var result = await CallDemographicsRequest<List<RethinkGuarantorDetails.ClientModel>>("/accounts/" + accountInfoId + "/users/primary?isGuarantor=true");

            return result;
        }

        public async Task<List<ClientDiagnosisModel>> GetClientDiagnosisAsync(int accountInfoId, int clientId)
        {
            var result = await CallMedicalRecordsRequest<ClientDiagnosisCodeForClaimWithoutAut>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/diagnoses");

            if (result != null)
            {
                return result.data.Select(x => new ClientDiagnosisModel
                {
                    diagnosisId = x.diagnosisId.Value,
                    diagnosisCode = x.diagnosis.diagnosisCode,
                    diagnosisFullDescription = x.diagnosis.description,
                    description = x.diagnosis.name,
                    order = x.order,
                    includeOnClaims = true
                }).ToList();
            }
            return null;
        }

        public async Task<ClientDiagnosisCodeForClaimWithoutAut> GetClientDiagnosisReturningModelAsync(int accountInfoId, int clientId)
        {
            var result = await CallMedicalRecordsRequest<ClientDiagnosisCodeForClaimWithoutAut>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/diagnoses");
            return result;
        }

        public async Task<List<ClientDiagnosisModel>> GetClientDiagnosisListByDiagnosisIdAsync(int accountInfoId, int clientId, int clientDiagnosisId)
        {
            var result = await CallMedicalRecordsRequest<ClientDiagnosisCodeForClaimWithoutAut>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/diagnoses/" + clientDiagnosisId);

            if (result != null)
            {
                return result.data
                .Select(x => new ClientDiagnosisModel
                {
                    diagnosisId = x.diagnosisId.Value,
                    diagnosisCode = x.diagnosis.diagnosisCode,
                    diagnosisFullDescription = x.diagnosis.description,
                    description = x.diagnosis.name,
                    order = x.order,
                    includeOnClaims = true
                }
                ).ToList();
            }

            return null;
        }

        public async Task<Diagnosis> GetDiagnosisByCodeAsync(int accountInfoId, string diagnosisCode)
        {
            var result = await CallMedicalRecordsRequest<ClientRethinkDiagnosisModel>("/accounts/" + accountInfoId + "/diagnoses?code=" + diagnosisCode);

            if (result != null)
            {
                var clientDiagnosis = result.data.Where(x => x.diagnosisCode == diagnosisCode).FirstOrDefault();
                return clientDiagnosis;
            }
            return null;
        }

        public async Task<List<Diagnosis>> GetAllDiagnosisAsync(int accountInfoId)
        {
            var result = await CallMedicalRecordsRequest<ClientRethinkDiagnosisModel>("/accounts/" + accountInfoId + "/diagnoses?take=99999");
            return result != null ? result.data : null;
        }

        public async Task<Diagnosis> GetDiagnosisByNameAsync(int accountInfoId, string searchTerm)
        {
            var result = await CallMedicalRecordsRequest<ClientRethinkDiagnosisModel>("/accounts/" + accountInfoId + "/diagnoses?name=" + searchTerm);

            if (result != null)
            {
                var clientDiagnosis = result.data.Where(x => x.name == searchTerm).FirstOrDefault();
                return clientDiagnosis;
            }

            return null;
        }

        // returns AccountInfo
        public async Task<AccountInfoEntityModel> GetAccountReturningEntityAsync(int accountInfoId, bool isChDetailsRequired = false)
        {
            var resMember = new AccountInfoEntityModel();
            var resAccountMember = await CallAccountsRequest<AccountModel>("/accounts/" + accountInfoId);

            if (resAccountMember != null)
            {
                resMember.Id = resAccountMember.id;
                resMember.Name = resAccountMember.name;
                resMember.AccountType = resAccountMember.isTestAccount ? 1 : 0;
                resMember.ClearingHouseId = resAccountMember.clearingHouseId;
                resMember.AccountAddress1 = resAccountMember.accountAddress.address1;
                resMember.AccountAddress2 = resAccountMember.accountAddress.address2;
                resMember.AccountAddress3 = resAccountMember.accountAddress.address3;
                resMember.AccountCity = resAccountMember.accountAddress.city;
                resMember.AccountStateId = resAccountMember.accountAddress.stateId;
                resMember.AccountTown = resAccountMember.accountAddress.town;
                resMember.AccountZip = resAccountMember.accountAddress.zipCode;
                resMember.BillingAddress1 = resAccountMember.billingAddress.address1;
                resMember.BillingAddress2 = resAccountMember.billingAddress.address2;
                resMember.BillingAddress3 = resAccountMember.billingAddress.address3;
                resMember.BillingFirstname = resAccountMember.billingName.firstName;
                resMember.BillingLastname = resAccountMember.billingName.lastName;
                resMember.BillingStateId = resAccountMember.billingAddress.stateId;
                resMember.BillingTown = resAccountMember.billingAddress.town;
                resMember.BillingZip = resAccountMember.billingAddress.zipCode;
                resMember.BillingCity = resAccountMember.billingAddress.city;
                resMember.BillingCountryId = resAccountMember.billingAddress.countryId;
                resMember.TimezoneId = resAccountMember.hcTimezoneId;
                resMember.TestAcct = resAccountMember.isTestAccount;
                resMember.NationalProviderId = resAccountMember.nationalProviderId;
                resMember.FederalTaxId = resAccountMember.federalTaxId;
                resMember.ProviderLogo = resAccountMember.providerLogo;
                resMember.Website = resAccountMember.website;
                resMember.FaxNumber = resAccountMember.faxNumber;
                resMember.PhoneNumber = resAccountMember.phoneNumber;
                resMember.Email = resAccountMember.emailAddress;
                resMember.AccountOrganizationTypeId = resAccountMember.organizationTypeId;
                resMember.DateCreated = resAccountMember.metaData.createdOn;
                resMember.CreatedBy = resAccountMember.metaData.createdBy;
                resMember.DateDeleted = resAccountMember.metaData.deletedOn;
                resMember.DeletedBy = resAccountMember.metaData.deletedBy;
                resMember.DateLastModified = resAccountMember.metaData.modifiedOn;
                resMember.ModifiedBy = resAccountMember.metaData.modifiedBy;
                resMember.IsParentVerificationRequired = (bool)resAccountMember.accountOptions.FirstOrDefault(x => x.type == "ParentVerificationRequired").value;
                resMember.IsStaffVerificationRequired = (bool)resAccountMember.accountOptions.FirstOrDefault(x => x.type == "StaffVerificationRequired").value;
                resMember.IsSessionNoteEnteredRequired = (bool)resAccountMember.accountOptions.FirstOrDefault(x => x.type == "SessionNoteRequired").value;
                resMember.tProId = resAccountMember.tProId;
                resMember.subscriptionFeatures = resAccountMember.subscriptionFeatures;
                resMember.subscriptionOptions = resAccountMember.subscription.subscriptionOptions;
            }


            if (isChDetailsRequired && resMember.ClearingHouseId > 0)
            {
                var details = await GetClearingHouseDetails();
                if (details.Data.Any())
                {
                    var chDetails = details.Data.Where(x => x.id == resMember.ClearingHouseId).FirstOrDefault();
                    if (chDetails != null)
                    {
                        resMember.ClearingHouse = chDetails;
                    }
                }
            }

            return resMember;
        }

        public async Task<RethinkAccountMember> GetMemberAsync(int accountInfoId, int memberId)
        {
            var result = await CallAccountsRequest<RethinkAccountMember>("/accounts/" + accountInfoId + "/members/" + memberId);
            return result;
        }

        public async Task<RethinkAccountMembersListModel> GetMemberListAsync(int accountInfoId)
        {
            var result = await CallAccountsRequest<RethinkAccountMembersListModel>("/accounts/" + accountInfoId + "/members?take=1000");
            return result;
        }
        public async Task<RethinkAccountMembersListModel> GetMembersAsync(int accountInfoId, string memberIds)
        {
            var result = await CallAccountsRequest<RethinkAccountMembersListModel>("/accounts/" + accountInfoId + "/members?" + memberIds);
            return result;
        }
        public async Task<ClientUserModel> GetChildProfile(int accountInfoId, int clientId)
        {
            var result = await CallDemographicsRequest<ClientUserModel>("/accounts/" + accountInfoId + "/users/types/client/" + clientId);
            return result;
        }

        public async Task<InsuranceContactsModel> GetInsuranceContactsIds(int accountInfoId, int clientId)
        {
            var result = await CallDemographicsRequest<InsuranceContactsModel>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/contacts");
            return result;
        }

        public async Task<InsuranceContacts> GetContactGuarantorDetails(int accountInfoId, int clientId)
        {
            var result = await CallDemographicsRequest<List<InsuranceContacts>>($"/accounts/{accountInfoId}/users/primary?isGuarantor=true");
            return result?.FirstOrDefault(r=>r.UserId == clientId);
        }

        public async Task<InsuranceContactsTypeModel> GetInsuranceContactsType(int accountInfoId, int childProfileId, int contactId)
        {
            var result = await CallHealthPlansRequest<InsuranceContactsTypeModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/contacts/" + contactId + "/insurance");
            return result;
        }

        public async Task<List<ChildProfileEntityModel>> GetChildProfilesForAccount(int accountInfoId)
        {
            var clientPatientModelResponse = await CallDemographicsRequest<ClientPatientModel>("/accounts/" + accountInfoId + "/users/types/client?take=99999");
            var clientPatientsDetail = clientPatientModelResponse.Data;
            if (clientPatientsDetail?.Count > 0)
            {
                var resChildProfileModelList = new List<ChildProfileEntityModel>();
                foreach (var item in clientPatientsDetail)
                {
                    var resChildProfileModel = new ChildProfileEntityModel();
                    resChildProfileModel.Id = item.id;
                    resChildProfileModel.MiddleName = item.name.middleName;
                    resChildProfileModel.LastName = item.name.lastName;
                    resChildProfileModel.FirstName = item.name.firstName;
                    resChildProfileModel.GenderId = item.genderId;
                    resChildProfileModel.MemberId = item.memberId;
                    resChildProfileModel.AccountInfoId = item.accountId;
                    resChildProfileModel.DateOfBirth = item.dateOfBirth;
                    resChildProfileModel.City = item.address.city;
                    resChildProfileModel.CountryId = item.address.countryId;
                    resChildProfileModel.Country = item.address.country;
                    resChildProfileModel.StateId = item.address.stateId;
                    resChildProfileModel.State = item.address.state;
                    resChildProfileModel.Town = item.address.town;
                    resChildProfileModel.ZipCode = item.address.zipCode;
                    resChildProfileModel.CreatedBy = item.metaData.createdBy;
                    resChildProfileModel.DateCreated = item.metaData.createdOn;
                    resChildProfileModel.DateDeleted = item.metaData.deletedOn;
                    resChildProfileModel.DateLastModified = item.metaData.modifiedOn;
                    resChildProfileModel.DateDeleted = item.metaData.deletedOn;
                    resChildProfileModel.DeletedBy = item.metaData.deletedBy;
                    resChildProfileModel.ModifiedBy = item.metaData.modifiedBy;
                    resChildProfileModel.ZipCode = item.address.zipCode;
                    resChildProfileModel.CountryId = item.address.countryId;
                    resChildProfileModel.Address = item.address.street1;
                    resChildProfileModel.Address2 = item.address.street2;
                    resChildProfileModel.UCI = item?.identifiers?.FirstOrDefault(x => x.identifierType == "Uci")?.value;
                    resChildProfileModel.Email = item?.identifiers?.FirstOrDefault(x => x.identifierType == "Email")?.value;
                    resChildProfileModel.ChildProfileContacts = item.contacts.Select(item2 => new ChildProfileContactEntityModel
                    {
                        TimezoneId = item2.timezoneId,
                        MemberId = item.memberId,
                        DateCreated = item2.metaData.createdOn,
                        DateDeleted = item2.metaData.deletedOn,
                        DateLastModified = item2.metaData.modifiedOn,
                        DeletedBy = item2.metaData.deletedBy,
                        ModifiedBy = item2.metaData.modifiedBy,
                        CreatedBy = item2.metaData.createdBy,
                        MaritalStatusId = item2.maritalStatusId,
                        DOB = item2.dateOfBirth,
                        GenderId = item2.genderId,
                        Id = item2.id,

                    }).ToList();
                    var attr = item.attributes.FirstOrDefault(x => x.type.ToLower() == "facilityid");
                    resChildProfileModel.FacilityId = attr != null ? Convert.ToInt32(attr.value) : 0;
                    resChildProfileModelList.Add(resChildProfileModel);
                }
                return resChildProfileModelList;
            }
            return null;
        }

        public async Task<ChildProfileEntityModel> GetChildProfileReturningEntity(int accountInfoId, int clientId)
        {
            var resChildProfile = await CallDemographicsRequest<ClientUserModel>("/accounts/" + accountInfoId + "/users/types/client/" + clientId);

            if (resChildProfile != null)
            {
                var resChildProfileModel = new ChildProfileEntityModel();
                resChildProfileModel.Id = resChildProfile.id;
                resChildProfileModel.MiddleName = resChildProfile.name.middleName;
                resChildProfileModel.LastName = resChildProfile.name.lastName;
                resChildProfileModel.FirstName = resChildProfile.name.firstName;
                resChildProfileModel.GenderId = resChildProfile.genderId;
                resChildProfileModel.MemberId = resChildProfile.memberId;
                resChildProfileModel.AccountInfoId = resChildProfile.accountId;
                resChildProfileModel.DateOfBirth = resChildProfile.dateOfBirth;
                resChildProfileModel.City = resChildProfile.address.city;
                resChildProfileModel.CountryId = resChildProfile.address.countryId;
                resChildProfileModel.StateId = resChildProfile.address.stateId;
                resChildProfileModel.Town = resChildProfile.address.town;
                resChildProfileModel.ZipCode = resChildProfile.address.zipCode;
                resChildProfileModel.CreatedBy = resChildProfile.metaData.createdBy;
                resChildProfileModel.DateCreated = resChildProfile.metaData.createdOn;
                resChildProfileModel.DateDeleted = resChildProfile.metaData.deletedOn;
                resChildProfileModel.DateLastModified = resChildProfile.metaData.modifiedOn;
                resChildProfileModel.DateDeleted = resChildProfile.metaData.deletedOn;
                resChildProfileModel.DeletedBy = resChildProfile.metaData.deletedBy;
                resChildProfileModel.ModifiedBy = resChildProfile.metaData.modifiedBy;
                resChildProfileModel.ZipCode = resChildProfile.address.zipCode;
                resChildProfileModel.CountryId = resChildProfile.address.countryId;
                resChildProfileModel.Address = resChildProfile.address.street1;
                resChildProfileModel.Address2 = resChildProfile.address.street2;
                resChildProfileModel.UCI = resChildProfile?.identifiers?.FirstOrDefault(x => x.identifierType == "Uci")?.value;
                resChildProfileModel.ChildProfileContacts = resChildProfile.contacts.Select(item2 => new ChildProfileContactEntityModel
                {
                    TimezoneId = item2.timezoneId,
                    MemberId = resChildProfile.memberId,
                    DateCreated = item2.metaData.createdOn,
                    DateDeleted = item2.metaData.deletedOn,
                    DateLastModified = item2.metaData.modifiedOn,
                    DeletedBy = item2.metaData.deletedBy,
                    ModifiedBy = item2.metaData.modifiedBy,
                    CreatedBy = item2.metaData.createdBy,
                    MaritalStatusId = item2.maritalStatusId,
                    DOB = item2.dateOfBirth,
                    GenderId = item2.genderId,
                    Id = item2.id,

                }).ToList();
                var attr = resChildProfile.attributes.FirstOrDefault(x => x.type.ToLower() == "facilityid");
                resChildProfileModel.FacilityId = attr != null ? Convert.ToInt32(attr.value) : 0;
                return resChildProfileModel;
            }
            return null;
        }

        public async Task<FunderListModel> GetFunderList(int accountInfoId)
        {
            var result = await CallHealthInsuranceRequest<FunderListModel>("/accounts/" + accountInfoId + "/funders?take=1000");
            return result;
        }

        public async Task<FunderListModel> GetFunderListByName(string funderName)
        {
            var returnData = new FunderListModel();
            var result = await CallHealthInsuranceRequest<FunderDataList>("/funders?name=" + funderName);

            if (result != null)
            {
                returnData.data = result.funders
                        .Select(x => new FunderModel
                        {
                            Id = x.id,
                            FunderName = x.funderName,
                            VendorId = x.vendorId,
                            accountId = x.accountId
                        }).ToList();

                return returnData;
            }
            return returnData;
        }

        public async Task<FunderListModel> GetFunderListByTaxId(string taxId)
        {
            var result = new FunderListModel();
            var data = await CallHealthInsuranceRequest<FunderDataDetails>("/funders/externalid?vendorid=" + taxId);

            if (data != null)
            {
                var funder = new FunderModel()
                {
                    Id = data.funder.id,
                    FunderName = data.funder.funderName,
                    VendorId = data.funder.vendorId,
                    accountId = data.funder.accountId
                };

                result.data = new List<FunderModel>() { funder };
                return result;
            }

            return result;
        }

        public async Task<List<ClientUserContact>> GetInsuranceContactByPolicy(string policyNo)
        {
            var result = await CallDemographicsRequest<List<ClientUserContact>>("/users/types/client/insurancecontacts?policyNumber=" + policyNo);
            return result;
        }



        public async Task<ChildProfileFunderResponseModel> GetChildProfileFunderMappings(int accountInfoId, int childProfileId)
        {
            var result = await CallHealthPlansRequest<ChildProfileFunderResponseModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/funderMappings");
            return result;
        }

        [Obsolete]
        public async Task<List<ChildProfileRethinkModel>> ExistingGetChildProfile(int accountInfoId)
        {
            //Existing API
            var result = await CallDemographicsRequest<ClientListUserModel>("/accounts/" + accountInfoId + "/users/types/client?take=1000");

            if (result != null)
            {
                return result.data
                        .Select(x => new ChildProfileRethinkModel
                        {
                            Id = x.id,
                            FirstName = x.name.firstName,
                            MiddleName = x.name.middleName,
                            LastName = x.name.lastName,
                            Name = x.name.firstName + " " + (x.name.middleName != null ? x.name.middleName + " " : "") + x.name.lastName
                        }).ToList();
            }

            return new List<ChildProfileRethinkModel>();
        }

        public async Task<List<ChildProfileRethinkModel>> GetChildProfile(int accountInfoId)
        {
            var result = await CallDemographicsRequest<ClientListUserModel>("/accounts/" + accountInfoId + "/users/types/client?take=1000");
            var data = result?.data;
            if (data is null || !data.Any())
                return new List<ChildProfileRethinkModel>();

            var list = new List<ChildProfileRethinkModel>(data.Count);            
            foreach (var item in data)
            {
                list.Add(item.ToChildProfileRethinkModel());
            }

            return list;

        }


        public async Task<ClientAuthorization> GetChildProfileAuthorizationById(int accountInfoId, int authorizationId)
        {
            var result = await CallHealthPlansRequest<ClientAuthorization>("/accounts/" + accountInfoId + "/users/types/client/all/authorizations/" + authorizationId);
            return result;
        }

        public async Task<ClientAuthorizationsModel> GetClientAuthorizationsByClientId(int accountInfoId, int clientId)
        {
            var result = await CallHealthPlansRequest<ClientAuthorizationsModel>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/authorizations?take=500");
            return result;
        }

        public async Task<ClientAuthorization> GetChildProfileAuthorizationByClientId(int accountInfoId, int clientId, int authorizationId)
        {
            var clientAuth = new ClientAuthorization();
            var response = string.Empty;

            var resUri = string.Empty;
            if (clientId != 0)
            {
                clientAuth = await CallHealthPlansRequest<ClientAuthorization>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/authorizations/" + authorizationId);
            }
            else
            {
                clientAuth = await CallHealthPlansRequest<ClientAuthorization>("/accounts/" + accountInfoId + "/users/types/client/all/authorizations/" + authorizationId);
            }

            return clientAuth;
        }

        public async Task<ClientDiagnosisCodeForClaimWithoutAut> GetClientDiagnosisByServiceId(int accountInfoId, int clientId, int serviceLineId)
        {
            var result = await CallMedicalRecordsRequest<ClientDiagnosisCodeForClaimWithoutAut>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/diagnoses?serviceLineId=" + serviceLineId + "&take=1000");
            return result;
        }


        public async Task<string> GetPropagatingAccountInfo(int accountInfoId)
        {
            var result = await CallAccountsRequest<PropagatingAccountInfo>("/accounts/propagating/" + accountInfoId);
            return result != null ? result.name : string.Empty;
        }

        public async Task<List<ChildProfileAuthorizationDiagnosisCode>> GetChildProfileAuthorizationDiagnosisCodesAsync(int accountInfoId, int childProfileId, int diagnosisId, int authId)
        {
            var result = new List<ChildProfileAuthorizationDiagnosisCode>();
            var diaCode = new ChildProfileAuthorizationDiagnosisCode();
            var data = await GetClientDiagnosisById(accountInfoId, childProfileId, diagnosisId);

            if (data == null)
                return [];

            diaCode.id = data.id;
            diaCode.diagnosisId = data.diagnosisId ?? 0;
            diaCode.metaData = data.metaData;
            diaCode.childProfileAuthorizationId = authId;
            diaCode.childProfileDiagnosisId = diagnosisId;
            diaCode.order = 1;
            diaCode.includeOnClaims = true;
            diaCode.Diagnosis = await GetDiagnosisById(data.diagnosisId ?? 0);

            result.Add(diaCode);
            return result;
        }

        public async Task<ClientDiagnosisCodes> GetClientDiagnosisById(int accountInfoId, int clientId, int clientDiagnosisId)
        {
            var result = await CallMedicalRecordsRequest<ClientDiagnosisCodes>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/diagnoses/" + clientDiagnosisId);
            return result;
        }

        public async Task<Diagnosis> GetDiagnosisById(int diagnosisId)
        {
            var result = await CallMedicalRecordsRequest<Diagnosis>("/definitions/diagnoses/" + diagnosisId);
            return result;
        }
        public async Task<DiagnosisEntityModel> GetClientDiagnosisByIdReturningEntityAsync(int clientDiagnosisId)
        {
            var resDiagnosisCodesEntity = new DiagnosisEntityModel();
            var resDiagnosisCodes = await GetDiagnosisById(clientDiagnosisId);
            resDiagnosisCodesEntity.Id = resDiagnosisCodes.id;

            if (resDiagnosisCodes.metaData != null)
            {
                resDiagnosisCodesEntity.DateDeleted = resDiagnosisCodes?.metaData.deletedOn;
                resDiagnosisCodesEntity.DeletedBy = resDiagnosisCodes.metaData.deletedBy;
                resDiagnosisCodesEntity.CreatedBy = resDiagnosisCodes.metaData.createdBy;
                resDiagnosisCodesEntity.DateCreated = resDiagnosisCodes.metaData.createdOn;
                resDiagnosisCodesEntity.ModifiedBy = resDiagnosisCodes.metaData.modifiedBy;
                resDiagnosisCodesEntity.DateLastModified = resDiagnosisCodes.metaData.modifiedOn;
            }
            resDiagnosisCodesEntity.DiagnosisCode = resDiagnosisCodes.diagnosisCode;
            resDiagnosisCodesEntity.Pos = resDiagnosisCodes.pos;
            resDiagnosisCodesEntity.Name = resDiagnosisCodes.name;
            resDiagnosisCodesEntity.Description = resDiagnosisCodes.description;
            resDiagnosisCodesEntity.TypeId = resDiagnosisCodes.diagnosisTypeId;

            return resDiagnosisCodesEntity;
        }

        public async Task<BillingCodeData> GetProviderBillingCode(int accountId, int billingCodeId)
        {
            var result = await CallHealthInsuranceRequest<BillingCodeData>("/accounts/" + accountId + "/billingcodes/" + billingCodeId);
            return result;
        }

        public async Task<List<RethinkProviderBillingCode>> GetProviderBillingCode(int accountId, string billingCode)
        {
            var result = await CallHealthInsuranceRequest<ClientBillingCodesModel>("/accounts/" + accountId + "/billingcodes?billingCode=" + billingCode);

            if (result != null)
            {
                return result.data
                        .Select(x => new RethinkProviderBillingCode
                        {
                            id = x.id,
                            billingCode = x.billingCode,
                            billingCode2 = x.billingCode2
                        }).ToList();
            }
            return new List<RethinkProviderBillingCode>();
        }

        public async Task<List<RethinkProviderBillingCode>> GetProviderBillingCodeList(int accountId)
        {
            var result = await CallHealthInsuranceRequest<ClientBillingCodesModel>("/accounts/" + accountId + "/billingcodes");

            if (result != null)
            {
                return result.data
                        .Select(x => new RethinkProviderBillingCode
                        {
                            id = x.id,
                            funderId = x.funderId,
                            rate = x.rate
                        }).ToList();
            }

            return new List<RethinkProviderBillingCode>();
        }

        public async Task<ClientBillingCodesModel> GetBillingCodeList(int accountId)
        {
            var result = await CallHealthInsuranceRequest<ClientBillingCodesModel>("/accounts/" + accountId + "/billingcodes?take=1000");
            return result;
        }

        public async Task<List<ClientReasonCodes>> GetReasonCodes()
        {
            var result = await CallHealthPlansRequest<List<ClientReasonCodes>>("/definitions/reasonCodes");
            return result;
        }
        public async Task<ClientProviderServiceModel> GetProviderService(int accountInfoId, int serviceId)
        {
            var result = await CallHealthInsuranceRequest<ClientProviderServiceModel>("/accounts/" + accountInfoId + "/services/" + serviceId);
            return result;
        }

        public async Task<List<ClientUnitTypes>> GetUnitTypesAsync()
        {
            var result = await CallHealthInsuranceRequest<List<ClientUnitTypes>>("/definitions/unittypes");
            return result;
        }

        public async Task<RethinkStaffMember> GetStaffMember(int accountInfoId, int staffMemberId)
        {
            var result = await CallDemographicsRequest<RethinkStaffMember>("/accounts/" + accountInfoId + "/users/types/staff/" + staffMemberId);

            if (result != null)
            {
                var timezones = await GetTimezones();
                result.Timezone = timezones.FirstOrDefault(x => x.id == result.timezoneId);
                return result;
            }
            return null;
        }

        public async Task<List<RethinkStaffMember>> GetStaffMemberList(int accountInfoId)
        {
            var result = await CallDemographicsRequest<RethinkStaffMemberList>("/accounts/" + accountInfoId + "/users/types/staff?take=1000&memberDeleted=false");
            return result != null ? result.data : null;
        }

        public async Task<List<RethinkStaffMembersByPermissionResponse>> GetStaffMemberListByPermission(int accountInfoId, List<string> permissions, string logicalOperator, int take=10000,int skip=0)
        {
            var permissionsQuery = string.Join("&", permissions.Select(p => $"permissions={p}"));
            var result = await CallDemographicsRequest<RethinkStaffMemberByPermission>("/accounts/" + accountInfoId + "/users/types/staff/by-permissions?" + permissionsQuery + "&logicOperator=" + logicalOperator + "&take=" + take + "&skip=" + skip + "");
            return result != null ? result.data : null;
        }

        public async Task<ProviderLocations> GetProviderLocation(int accountInfoId, int providerLocationId)
        {
            var result = await CallPracticeOperationsRequest<ProviderLocations>("/accounts/" + accountInfoId + "/providerLocations/" + providerLocationId);
            return result;
        }

        public async Task<ProviderLocations> GetMainLocation(int accountInfoId)
        {
            var result = await CallPracticeOperationsRequest<ProviderLocations>("/accounts/" + accountInfoId + "/providerLocations/main");
            return result;
        }

        public async Task<ClientProviderLocationsModel> GetProviderLocationList(int accountInfoId)
        {
            var result = await CallPracticeOperationsRequest<ClientProviderLocationsModel>("/accounts/" + accountInfoId + "/providerLocations");
            return result;
        }

        public async Task<ProviderBillingCodeCredentialModel> GetProviderBillingCodeCredential(int accountInfoId, int providerBillingCode, int providerBillingCodeCredentials)
        {
            var result = await CallHealthInsuranceRequest<ProviderBillingCodeCredentialModel>("/accounts/" + accountInfoId + "/providerBillingCodes/" + providerBillingCode + "/credentials/" + providerBillingCodeCredentials);
            return result;
        }

        public async Task<List<ReferringProviderDropdownModel>> GetReferringProvidersByClientId(int clientId, int accountInfoId)
        {
            var referringProviderList = new List<ReferringProviderDropdownModel>();

            var result = await CallPracticeOperationsRequest<ChildProfileReferringProviders>("/accounts/" + accountInfoId + "/users/types/client/" + clientId + "/referringproviders");

            if (result != null)
            {
                foreach (var item in result.childProfileReferringProviders.data)
                {
                    var refProviderObj = new ReferringProviderDropdownModel();
                    refProviderObj.Id = item.id;
                    refProviderObj.IsDefault = item.isDefault;
                    refProviderObj.IsActive = true;

                    var referringProviders = await CallDemographicsRequest<ReferringProviderNameModel>("/accounts/" + accountInfoId + "/users/types/referringProvider/" + item.referringProviderId);

                    if (referringProviders != null)
                    {
                        refProviderObj.ProviderName = referringProviders.name.firstName + " " + referringProviders.name.middleName + " " + referringProviders.name.lastName;
                        refProviderObj.FirstName = referringProviders.name.firstName;
                        refProviderObj.MiddleName = referringProviders.name.middleName;
                        refProviderObj.LastName = referringProviders.name.lastName;
                    }

                    referringProviderList.Add(refProviderObj);
                }
            }
            return referringProviderList;
        }

        public async Task<List<ChildProfileRethinkModel>> GetChildProfileByName(int accountInfoId, string searchString)
        {
            var searchstring = searchString.ToLower();
            var firstNameResult = await GetChildProfileByFirstName(accountInfoId, searchstring);
            var middleNameResult = await GetChildProfileByMiddleName(accountInfoId, searchstring);
            var lastNameResult = await GetChildProfileByLastName(accountInfoId, searchstring);
            firstNameResult.AddRange(middleNameResult.AsEnumerable());
            firstNameResult.AddRange(lastNameResult.AsEnumerable());
            return firstNameResult;
        }

        public async Task<List<ChildProfileRethinkModel>> GetChildProfileByFirstName(int accountInfoId, string firstName)
        {
            var result = await CallDemographicsRequest<ClientListUserModel>("/accounts/" + accountInfoId + "/users/types/client?firstName=" + firstName);

            if (result != null)
            {
                return result.data.Select(x => new ChildProfileRethinkModel
                {
                    Id = x.id,
                    FirstName = x.name.firstName,
                    MiddleName = x.name.middleName,
                    LastName = x.name.lastName
                }).ToList();
            }

            return new List<ChildProfileRethinkModel>();
        }
        public async Task<List<ChildProfileRethinkModel>> GetChildProfileByMiddleName(int accountInfoId, string middleName)
        {
            var result = await CallDemographicsRequest<ClientListUserModel>("/accounts/" + accountInfoId + "/users/types/client?middleName=" + middleName);

            if (result != null)
            {
                return result.data.Select(x => new ChildProfileRethinkModel
                {
                    Id = x.id,
                    FirstName = x.name.firstName,
                    MiddleName = x.name.middleName,
                    LastName = x.name.lastName
                }).ToList();
            }

            return new List<ChildProfileRethinkModel>();
        }
        public async Task<List<ChildProfileRethinkModel>> GetChildProfileByLastName(int accountInfoId, string lastname)
        {
            var result = await CallDemographicsRequest<ClientListUserModel>("/accounts/" + accountInfoId + "/users/types/client?lastname=" + lastname);

            if (result != null)
            {
                return result.data.Select(x => new ChildProfileRethinkModel
                {
                    Id = x.id,
                    FirstName = x.name.firstName,
                    MiddleName = x.name.middleName,
                    LastName = x.name.lastName
                }).ToList();
            }

            return new List<ChildProfileRethinkModel>();
        }
        public async Task<List<ClientTimezonesModel>> GetTimezones()
        {
            var result = await CallDemographicsRequest<List<ClientTimezonesModel>>("/definitions/timezones");
            return result;
        }

        public async Task<List<ServiceLines>> GetChildProfileFunderServiceLineMapping(int accountInfoId, int childProfileId)
        {
            var data = new List<ServiceLines>();
            var result = await CallHealthPlansRequest<ChildProfileFunderResponseModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/funderMappings");

            if (result != null)
            {
                foreach (var item in result.data?.Where(x => x.metaData.deletedOn == null))
                {
                    data.AddRange(await GetServiceLineMappingsByFunderId(accountInfoId, childProfileId, item.id));
                }
            }

            return data;
        }

        [Obsolete]
        // GET SERVICE LINE MAPPING LIST BASED ON FUNDER ID
        public async Task<List<ServiceLines>> ExistingGetServiceLineMappingsByFunderId(int accountInfoId, int childProfileId, int childProfileFunderMappingId)
        {
            //Existing GetServiceLineMappingsByFunderId
            var result = await CallHealthPlansRequest<ChildProfileServiceLineResponseModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/funderMappings/" + childProfileFunderMappingId + "/serviceLineMappings");

            if (result != null)
            {
                foreach (var item in result.data)
                {
                    item.ChildProfileFunderMapping = await GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, item.ChildProfileFunderMappingId);
                    item.responsibilitySequence = item.ChildProfileFunderMapping.insuranceType;
                    item.ChildProfileFunderMapping.Funder = await GetFunder(accountInfoId, item.ChildProfileFunderMapping.funderId);
                    item.ChildProfileFunderMapping.Funder.FunderInsurancePlans = await GetFunderInsurancePlansForFunder(accountInfoId, childProfileId, item.ChildProfileFunderMappingId);
                }
            }

            return result.data;
        }

        public async Task<List<ServiceLines>> GetServiceLineMappingsByFunderId(int accountInfoId, int childProfileId, int childProfileFunderMappingId)
        {
            var result = await CallHealthPlansRequest<ChildProfileServiceLineResponseModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/funderMappings/" + childProfileFunderMappingId + "/serviceLineMappings");
            if (result?.data == null || !result.data.Any())
                return result?.data ?? new List<ServiceLines>();

            var items = result.data;

            // Get distinct mapping ids to avoid duplicate calls
            var mappingIds = items
                .Select(x => x.ChildProfileFunderMappingId)
                .Distinct()
                .ToList();

            // Fetch mappings in parallel
            var mappingTasks = mappingIds.ToDictionary(
                id => id,
                id => GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, id));

            await Task.WhenAll(mappingTasks.Values);

            // Fetch dependent data in parallel
            var funderTasks = new Dictionary<int, Task<FunderDataModel>>();
            var insurancePlanTasks = new Dictionary<int, Task<List<FunderInsurancePlan>>>();

            foreach (var mapTask in mappingTasks)
            {
                var mapping = mapTask.Value.Result;

                funderTasks[mapping.funderId] =
                    GetFunder(accountInfoId, mapping.funderId);

                insurancePlanTasks[mapTask.Key] =
                    GetFunderInsurancePlansForFunder(accountInfoId, childProfileId, mapTask.Key);
            }

            await Task.WhenAll(funderTasks.Values.Concat<Task>(insurancePlanTasks.Values));

            // Assign results
            foreach (var item in items)
            {
                var mapping = mappingTasks[item.ChildProfileFunderMappingId].Result;

                item.ChildProfileFunderMapping = mapping;
                item.responsibilitySequence = mapping.insuranceType;

                mapping.Funder = funderTasks[mapping.funderId].Result;
                mapping.Funder.FunderInsurancePlans =
                    insurancePlanTasks[item.ChildProfileFunderMappingId].Result;
            }

            return items;
        }

        public async Task<ServiceLines> GetChildProfileFunderServiceLineMappingDataByClient(int accountInfoId, int childProfileId, int funderId, int serviceId)
        {
            var ClientFunderMappings = await GetChildProfileFunderMappings(accountInfoId, childProfileId);
            var ClientFunderMapping = ClientFunderMappings.data.FirstOrDefault(x => x.funderId == funderId);
            int fundermappingid = ClientFunderMapping.id;
            var serviceLineMapping = await GetServiceLineMappingsByFunderId(accountInfoId, childProfileId, fundermappingid);
            var funderServiceLineMappingId = serviceLineMapping.FirstOrDefault(x => x.serviceId == serviceId);
            return funderServiceLineMappingId;
        }

        public async Task<ChildProfileServiceLines> GetServiceLine(int accountInfoId, int servicelineMappingId)
        {
            var result = await CallHealthInsuranceRequest<ChildProfileServiceLines>("/accounts/" + accountInfoId + "/serviceLines/" + servicelineMappingId);
            return result;
        }

        public async Task<int> GetClearingHouseId(int accountInfoId)
        {
            var result = await CallAccountsRequest<AccountModel>("/accounts/" + accountInfoId);
            if (result != null)
            {
                var chId = result.clearingHouseId.ToString();
                if (!string.IsNullOrEmpty(chId))
                {
                    if (int.TryParse(chId, out _))
                    {
                        return Convert.ToInt32(chId);
                    }
                }
            }

            return 0;
        }

        public async Task<List<StateModel>> GetStateList()
        {
            var result = await CallDemographicsRequest<List<StateModel>>("/definitions/states");
            return result;
        }

        public async Task<StateModel> GetStateById(int stateId)
        {
            var result = await CallDemographicsRequest<List<StateModel>>("/definitions/states");
            if (result != null)
            {
                return result.FirstOrDefault(x => x.id == stateId);
            }
            return null;
        }
        public async Task<List<CountryModel>> GetCountryList()
        {
            var result = await CallDemographicsRequest<List<CountryModel>>("/definitions/countries");
            return result;
        }

        public async Task<CountryModel> GetCountryById(int countryId)
        {
            var result = await CallDemographicsRequest<List<CountryModel>>("/definitions/countries");
            if (result != null)
            {
                return result.FirstOrDefault(x => x.id == countryId);
            }
            return null;
        }

        public async Task<AppointmentClientAuthBillingCodeModel> GetChildProfileAuthBillingCodeForAppointment(int accountInfoId, int childProfileId, int billingCodeId)
        {
            var result = await CallHealthPlansRequest<AppointmentClientAuthBillingCodeModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/billingCodes/" + billingCodeId);
            return result;
        }

        public async Task<List<ClientAuthorizationBillingCodeModel>> GetClientAuthBillingCodesByAuthId(int accountInfoId, int childProfileId, int authId)
        {
            var billingCodes = new List<ClientAuthorizationBillingCodeModel>();
            var result = await CallHealthPlansRequest<ClientAuthBillingCodesModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/authorizations/" + authId + "/billingCodes");

            if (result != null)
            {
                foreach (var item in result.data)
                {
                    ClientAuthorizationBillingCodeModel clientAuthorizationBillingCodeModel = new ClientAuthorizationBillingCodeModel();
                    clientAuthorizationBillingCodeModel.Id = item.id;
                    clientAuthorizationBillingCodeModel.BillingCodeId = item.providerBillingCodeId;
                    clientAuthorizationBillingCodeModel.NoOfUnits = item.noOfUnits;
                    clientAuthorizationBillingCodeModel.UnitTypeId = item.unitTypeId;
                    clientAuthorizationBillingCodeModel.SchedulingGoalInterval = item.schedulingGoalFrequencyTypeId;
                    clientAuthorizationBillingCodeModel.Interval = item.frequencyTypeId;
                    clientAuthorizationBillingCodeModel.SchedulingGoalNoOfUnits = item.schedulingGoalNoOfUnits;
                    clientAuthorizationBillingCodeModel.ChildProfileAuthorizationId = item.childProfileAuthorizationId;

                    var providerServiceBillingCode = await GetProviderBillingCode(accountInfoId, item.providerBillingCodeId);

                    var providerBillingCode = new BillingCodeData();
                    providerBillingCode.id = providerServiceBillingCode.id;
                    providerBillingCode.billingCode2 = providerServiceBillingCode.billingCode2;
                    providerBillingCode.billingCodeText = providerServiceBillingCode.billingCode;
                    providerBillingCode.billingCode = providerServiceBillingCode.billingCode;
                    providerBillingCode.rate = providerServiceBillingCode.rate;
                    providerBillingCode.serviceId = providerServiceBillingCode.serviceId;
                    providerBillingCode.unitTypeId = providerServiceBillingCode.unitTypeId;

                    clientAuthorizationBillingCodeModel.ProviderBillingCode = providerBillingCode;

                    billingCodes.Add(clientAuthorizationBillingCodeModel);
                }

                return billingCodes;
            }

            return new List<ClientAuthorizationBillingCodeModel>();
        }

        public async Task<PayerDetailsModel> GetPayerDetails(int funderId)
        {
            var result = await CallHealthInsuranceRequest<List<PayerDetailsModel>>("/definitions/payers?funderId=" + funderId);

            if (result != null)
            {
                return result.FirstOrDefault();
            }
            return new PayerDetailsModel();
        }

        public async Task<List<EraLocationCheckModel>> GetAccountInfoByTaxIDNPI(string taxId, string npi)
        {
            var resAccUrl = "";
            if (string.IsNullOrEmpty(taxId))
                resAccUrl = $"/providerlocations/externalid?npinumber={npi}";
            else if (string.IsNullOrEmpty(npi))
                resAccUrl = $"/providerlocations/externalid?federalTaxId={taxId}";
            else if (!string.IsNullOrEmpty(taxId) && !string.IsNullOrEmpty(npi))
                resAccUrl = $"/providerlocations/externalid?npinumber={npi}&federalTaxId={taxId}";

            var result = await CallPracticeOperationsRequest<List<ProviderLocations>>(resAccUrl);

            if (result != null)
            {
                return result.Select(x => new EraLocationCheckModel
                {
                    id = x.id,
                    accountId = x.accountId
                }).ToList();
            }
            return null;
        }

        public async Task<List<EraLocationCheckModel>> GetAccountByName(string name)
        {
            var result = await CallAccountsRequest<AccountListModel>($"/accounts?name={name}");

            if (result != null)
            {
                return result.data.Select(x => new EraLocationCheckModel
                {
                    id = 0,
                    accountId = x.id
                }).ToList();
            }
            return null;
        }


        public async Task<List<FunderModel>> GetFunderInfoByTaxID(int accountInfoId, string taxId)
        {
            var result = await CallHealthInsuranceRequest<List<FunderModel>>("/accounts/" + accountInfoId + "/funders?vendorId=" + taxId);
            return result;
        }

        #endregion

        #region "Entities"
        //**************ENTITY START**********************************************************************************************************************
        // RETURNS CHILD PROFILE FUNDER MAPPING ENTITY
        public async Task<FunderDetails> GetChildProfileFunderMappingByMappingId(int accountInfoId, int childProfileId, int mappingId)
        {
            var result = await CallHealthPlansRequest<FunderDetails>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/funderMappings/" + mappingId);

            if (result != null)
            {
                result.InsuranceContact = await GetInsuranceContactEntity(accountInfoId, childProfileId, result.childProfileInsuranceContactId);
                if (result.InsuranceContact == null)
                {
                    throw new Exception("Insurance Contact not found for Child Profile Funder Mapping Id: " + mappingId);
                }
                result.InsuranceContact.InsuranceContactsType = await GetInsuranceContactsType(accountInfoId, childProfileId, result.childProfileInsuranceContactId);
                return result;
            }
            return null;
        }

        //RETURNS FUNDER ENTITY
        public async Task<FunderDataModel> GetFunder(int accountInfoId, int funderId)
        {
            var result = await CallHealthInsuranceRequest<FunderDataDetails>("/accounts/" + accountInfoId + "/funders/" + funderId);
            return result != null ? result.funder : null;
        }

        public async Task<List<FunderDataModel>> GetAllFundersForAccount(int accountInfoIf)
        {
            var result = await CallHealthInsuranceRequest<FunderDataDetailsList>("/accounts/" + accountInfoIf + "/funders?take=99999");
            return result != null ? result.Data : null;
        }

        //RETURNS CHILD PROFILE FUNDER SERVICE LINE MAPPING ENTITY
        public async Task<ServiceLines> GetChildProfileFunderServiceLineMappingEntity(int accountInfoId, int childProfileId, int childProfileFunderMappingId, int servicelineMappingId)
        {
            var result = await CallHealthPlansRequest<ServiceLines>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/funderMappings/" + childProfileFunderMappingId + "/serviceLineMappings/" + servicelineMappingId);
            if (result != null)
            {
                result.ChildProfileFunderMapping = await GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, result.ChildProfileFunderMappingId);
                return result;
            }

            return null;
        }


        //RETURNS INSURANCE CONTACT ENTITY
        public async Task<InsuranceContacts> GetInsuranceContactEntity(int accountInfoId, int childProfileId, int insuranceContactId)
        {
            var result = await CallDemographicsRequest<InsuranceContacts>("/accounts/" + accountInfoId + "/users/types/10/" + childProfileId + "/contacts/" + insuranceContactId);
            return result;
        }

        //RETURNS SERVICE FUNDERS ENTITY
        public async Task<ServiceFunderData> GetServiceFundersEntityById(int accountInfoId, int serviceFunderId)
        {
            var result = await CallHealthInsuranceRequest<ServiceFunderData>("/accounts/" + accountInfoId + "/serviceFunders/" + serviceFunderId);
            return result;
        }

        //RETURNS SERVICE FUNDERS ENTITY LIST BY FUNDER ID
        public async Task<List<ServiceFunderData>> GetServiceFundersEntityListByFunderId(int accountInfoId, int childProfileId, int funderId)
        {
            var result = await CallHealthInsuranceRequest<ServiceFunderDetails>("/accounts/" + accountInfoId + "/serviceFunders?funderId=" + funderId);
            return result != null ? result.data : null;
        }

        //RETURNS CHILDPROFILE REFERRING PROVIDER ENTITY
        public async Task<clientReferringProviders> GetChildProfileReferringProviderEntity(int accountInfoId, int childProfileId, int referringProviderId)
        {
            var result = await CallPracticeOperationsRequest<clientReferringProviders>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/referringProviders/" + referringProviderId);

            if (result != null)
            {
                result.ReferringProvider = await GetReferringProviderInfo(accountInfoId, childProfileId, result.referringProviderId);
                return result;
            }

            return new clientReferringProviders();
        }

        //RETURNS CHILDPROFILE REFERRING PROVIDER ENTITY
        public async Task<ReferringProvidersModel> GetReferringProviderInfo(int accountInfoId, int childProfileId, int referringProviderId)
        {
            var result = await CallDemographicsRequest<ReferringProvidersModel>("/accounts/" + accountInfoId + "/users/types/referringProvider/" + referringProviderId);
            return result;
        }

        public async Task<List<FunderInsurancePlan>> GetFunderInsurancePlansForFunder(int accountInfoId, int childProfileId, int clientFunderId)
        {
            var result = await CallHealthInsuranceRequest<FunderInsurancePlanModel>("/accounts/" + accountInfoId + "/funders/" + clientFunderId + "/insurancePlans");
            return result != null ? result.data : new List<FunderInsurancePlan>();
        }

        public async Task<ClientListUserModel> GetAllRenderingProvidersAsync(int accountInfoId)
        {
            var result = await CallDemographicsRequest<ClientListUserModel>("/accounts/" + accountInfoId + "/users/types/20?take=1000&memberDeleted=false");
            return result;
        }


        public async Task<List<AuthRenderingProviderType>> GetRenderingProvidersAsync(int accountInfoId, bool withNpi = false)
        {
            var result = await GetAllRenderingProvidersAsync(accountInfoId);

            if (result != null)
            {
                withNpi = false;

                if (withNpi) result.data = result.data.Where(x => x.identifiers.All(y => x.identifiers.Any(f => f.identifierType.ToLower() == "npinumber"))).ToList();

                var data = result.data
                    .Select(x => new AuthRenderingProviderType
                    {
                        Id = x.id,
                        //Name = $"{x.name.firstName} {x.name.lastName}",
                        Name = $"{x.name.firstName} {x.name.lastName}{(withNpi ? $" - {x.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber").value}" : string.Empty)}",
                        StaffMemberId = x.memberId
                    })
                    .OrderBy(x => x.Name)
                    .ToList();

                // COMMENTED AS THIS IS GIVING ERROR
                //result.Insert(0, new AuthRenderingProviderType { StaffMemberId = -2, Name = "Provider Assigned to Appointment", Id = 2 });
                //result.Insert(1, new AuthRenderingProviderType { StaffMemberId = -1, Name = "Agency", Id = 1 });
                return data;
            }
            return new List<AuthRenderingProviderType>();
        }



        //**************ENTITY END***********************************************************************************************************************************************
        #endregion

        #region "Appointment"
        public async Task<AppointmentRethinkModel> GetAppointmentAsync(int appointmentId)
        {
            var result = await CallAppointmentsRequest<AppointmentRethinkModel>("/" + appointmentId);
            return result;
        }

        public async Task<List<AppointmentRethinkModel>> GetAppointmentListAsync(List<int> appointmentIds)
        {
            var resAppointment = new List<AppointmentRethinkModel>();
            var childAppointmentResponse = string.Empty;
            var resAppointmentURL = _httpAppointmentClient.BaseAddress + "/list";
            using (var resAppointmentClient = new HttpClient())
            {
                _httpAppointmentClient.DefaultRequestHeaders.ToList().ForEach(header =>
                {
                    resAppointmentClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                });

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(resAppointmentURL),
                    Content = new StringContent(JsonConvert.SerializeObject(appointmentIds), Encoding.UTF8, MediaTypeNames.Application.Json), // or "application/json" in older versions
                };

                HttpResponseMessage childAppointmentStatus = await resAppointmentClient.SendAsync(request);
                if (childAppointmentStatus.IsSuccessStatusCode)
                {
                    childAppointmentResponse = await childAppointmentStatus.Content.ReadAsStringAsync();
                    resAppointment = JsonConvert.DeserializeObject<List<AppointmentRethinkModel>>(childAppointmentResponse, settings);
                }
            }
            return resAppointment;
        }

        public async Task<List<AppointmentRethinkModel>> GetCompletedAppointmentListAsync(int accountInfoId, int clientId, DateTime startDate)
        {
            var result = await CallAppointmentsRequest<List<int>>("/client/" + clientId + "/completed?startDate=" + startDate.ToString("yyyy-MM-dd"));
            if (result != null)
            {
                var apptList = await GetAppointmentListAsync(result);
                return apptList;
            }
            return new List<AppointmentRethinkModel>();
        }


        public async Task<PropagatingStaffMember> GetPropagatingStaffMemberById(int propStaffId)
        {
            var result = await CallPracticeOperationsRequest<PropagatingStaffMember>("/users/types/staff/propagating/" + propStaffId);
            return result;
        }

        public async Task<AppointmentWorkFlowHistoyModel> GetWorkFlowHistoyDetailsById(int workFlowHistoryId)
        {
            var result = await CallAppointmentsRequest<AppointmentWorkFlowHistoyModel>("/status/" + workFlowHistoryId);
            return result;
        }

        public async Task<ProviderLocationModel> GetChildProfileFacility(int accountInfoId, int childProfileId)
        {
            var result = await CallPracticeOperationsRequest<ProviderLocationModel>("/accounts/" + accountInfoId + "/users/types/client/" + childProfileId + "/facility");
            return result;
        }

        public async Task<List<AccountModel>> GetBillingAccountsAsync()
        {
            //Filter parameters
            var filterQuery = new Dictionary<string, string>
            {
                { "showBilling", "true" },
                { "billingOptionId", "1" },
                { "memberdeleted", "false" }
            };

            return await CallGenericRequest<AccountModel>(_httpAccountsClient, "/accounts/?", filterQuery);
        }


        /// <summary>
        /// Gets all completed appointments for an account within a specified date range.
        /// </summary>
        /// <param name="accountInfoId">The ID of the account.</param>
        /// <param name="fromDate">The start date of the range.</param>
        /// <param name="toDate">The end date of the range.</param>
        /// <returns>A list of completed appointments for the account within the specified date range.</returns>
        public async Task<List<int>> GetAllCompletedAppointmentsForAnAccountAsync(int accountInfoId, DateTime fromDate, DateTime toDate, int AppointmentTypeId = 1)
        {
            return await CallAppointmentsRequest<List<int>>("/completed/daterange?accountId=" + accountInfoId + "&startDate=" + fromDate.ToString("yyyy-MM-dd") + "&endDate=" + toDate.ToString("yyyy-MM-dd") + "&appointmentTypeId=" + AppointmentTypeId);
        }
        #endregion

        #region Base Functions
        JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        private async Task<List<T>> CallGenericRequest<T>(HttpClient _httpClient, string url, Dictionary<string, string> filterQuery, int skip = 0, int take = 5)
        {
            int total = 0;
            string filters = string.Empty;
            var result = new List<T>();

            foreach (var filter in filterQuery)
            {
                filters += filter.Key + "=" + filter.Value + "&";
            }

            do
            {
                var queryString = filters + "skip=" + skip + "&take=" + take;
                var response = await _httpClient.GetAsync(_httpClient.BaseAddress + url + queryString);
                var pagedResponse = await ReturnGenericPagedData<T>(response);
                if (total == 0) { total = pagedResponse.Total; }
                result.AddRange(pagedResponse.Data ?? []);
                skip += take;
                total -= take;
            }
            while (total > 0);

            return result;
        }

        private async Task<T> CallAccountsRequest<T>(string url) =>
            await GetCachedOrHttpAsync("accounts", _httpAccountsClient, url, () => HttpGetAsync<T>(_httpAccountsClient, url));

        private async Task<T> CallCurriculumRequest<T>(string url) =>
            await GetCachedOrHttpAsync("curriculum", _httpCurriculumClient, url, () => HttpGetAsync<T>(_httpCurriculumClient, url));

        private async Task<T> CallDemographicsRequest<T>(string url) =>
            await GetCachedOrHttpAsync("demographics", _httpDemographicsClient, url, () => HttpGetAsync<T>(_httpDemographicsClient, url));

        private async Task<T> CallHealthPlansRequest<T>(string url) =>
            await GetCachedOrHttpAsync("healthplans", _httpHealthPlansClient, url, () => HttpGetAsync<T>(_httpHealthPlansClient, url));

        private async Task<T> CallHealthInsuranceRequest<T>(string url) =>
            await GetCachedOrHttpAsync("healthinsurance", _httpHealthInsuranceClient, url, () => HttpGetAsync<T>(_httpHealthInsuranceClient, url));

        private async Task<T> CallMedicalRecordsRequest<T>(string url) =>
            await GetCachedOrHttpAsync("medicalrecords", _httpMedicalRecordsClient, url, () => HttpGetAsync<T>(_httpMedicalRecordsClient, url));

        private async Task<T> CallPracticeOperationsRequest<T>(string url) =>
            await GetCachedOrHttpAsync("practiceops", _httpPracticeOperationsClient, url, () => HttpGetAsync<T>(_httpPracticeOperationsClient, url));

        private async Task<T> CallAppointmentsRequest<T>(string url) =>
            await GetCachedOrHttpAsync("appointments", _httpAppointmentClient, url, () => HttpGetAsync<T>(_httpAppointmentClient, url));

        private async Task<T> GetCachedOrHttpAsync<T>(string serviceName, HttpClient client, string relativePath, Func<Task<T>> httpGet)
        {
            if (!TryGetSessionCacheContext(relativePath, out var sessionKey, out var accountId))
            {
                return await httpGet();
            }

            var cachePath = $"{serviceName}:{relativePath}";
            try
            {
                return await _sessionCache!.GetOrFetchAsync(sessionKey, accountId, cachePath, httpGet);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Session master data cache read failed; calling upstream. Path={Path}", cachePath);
                return await httpGet();
            }
        }

        private bool TryGetSessionCacheContext(string relativePath, out string sessionKey, out int accountId)
        {
            sessionKey = string.Empty;
            accountId = 0;
            if (_sessionCache == null || _requestContext == null)
            {
                return false;
            }

            sessionKey = _requestContext.SessionKey ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                return false;
            }

            if (_requestContext.AccountInfoId is int aid && aid > 0)
            {
                accountId = aid;
                return AccountScopedPathMatches(relativePath, accountId);
            }

            if (TryParseAccountFromPath(relativePath, out var parsedId))
            {
                accountId = parsedId;
                return true;
            }

            return false;
        }

        private static bool AccountScopedPathMatches(string relativePath, int accountInfoId)
        {
            return relativePath.Contains("/accounts/" + accountInfoId + "/", StringComparison.Ordinal)
                   || relativePath.StartsWith("/accounts/" + accountInfoId + "?", StringComparison.Ordinal)
                   || relativePath == "/accounts/" + accountInfoId;
        }

        private static bool TryParseAccountFromPath(string relativePath, out int accountInfoId)
        {
            accountInfoId = 0;
            const string prefix = "/accounts/";
            var idx = relativePath.IndexOf(prefix, StringComparison.Ordinal);
            if (idx < 0)
            {
                return false;
            }

            var start = idx + prefix.Length;
            var end = start;
            while (end < relativePath.Length && char.IsDigit(relativePath[end]))
            {
                end++;
            }

            if (end == start)
            {
                return false;
            }

            return int.TryParse(relativePath.AsSpan(start, end - start), out accountInfoId) && accountInfoId > 0;
        }

        private async Task<T> HttpGetAsync<T>(HttpClient client, string url)
        {
            var response = await client.GetAsync(client.BaseAddress + url);
            return await ReturnGenericData<T>(response);
        }

        private async Task<T> ReturnGenericData<T>(HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
            {
                var responseContent = await message.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<T>(responseContent, settings);
                return json;
            }
            return (T)Convert.ChangeType(null, typeof(T));
        }

        private async Task<PagedResponse<T>> ReturnGenericPagedData<T>(HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
            {
                var responseContent = await message.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<PagedResponse<T>>(responseContent, settings);
                return json;
            }
            return (PagedResponse<T>)Convert.ChangeType(null, typeof(T));
        }
        #endregion
    }
}
