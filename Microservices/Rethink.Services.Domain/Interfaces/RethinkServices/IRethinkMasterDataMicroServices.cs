using BillingService.Domain.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Interfaces
{
    public interface IRethinkMasterDataMicroServices
    {
        Task<List<ClientDiagnosisModel>> GetClientDiagnosisAsync(int accountInfoId, int clientId);
        Task<Diagnosis> GetDiagnosisByNameAsync(int accountInfoId, string searchTerm);
        Task<RethinkAccountMember> GetMemberAsync(int accountInfoId, int memberId);
        Task<List<Diagnosis>> GetAllDiagnosisAsync(int accountInfoId);
        Task<ClientUserModel> GetChildProfile(int accountInfoId, int clientId);
        Task<FunderDataModel> GetFunder(int accountInfoId, int funderId);

        Task<List<FunderDataModel>> GetAllFundersForAccount(int accountInfoIf);
        Task<List<ChildProfileRethinkModel>> GetChildProfile(int accountInfoId);

        Task<ClientAuthorization> GetChildProfileAuthorizationById(int accountInfoId, int authorizationId);
        Task<ClientAuthorizationsModel> GetClientAuthorizationsByClientId(int accountInfoId, int clientId);
        Task<ClientAuthorization> GetChildProfileAuthorizationByClientId(int accountInfoId, int clientId, int authorizationId);
        Task<string> GetPropagatingAccountInfo(int accountInfoId);
        Task<Diagnosis> GetDiagnosisByCodeAsync(int accountInfoId, string diagnosisCode);
        Task<BillingCodeData> GetProviderBillingCode(int accountId, int billingCodeId);
        Task<List<RethinkProviderBillingCode>> GetProviderBillingCode(int accountId, string billingCode);
        Task<List<RethinkProviderBillingCode>> GetProviderBillingCodeList(int accountId);
        Task<List<ClientReasonCodes>> GetReasonCodes();
        Task<ClientProviderServiceModel> GetProviderService(int accountInfoId, int serviceId);
        Task<List<ClientUnitTypes>> GetUnitTypesAsync();
        Task<RethinkStaffMember> GetStaffMember(int accountInfoId, int staffMemberId);
        Task<List<RethinkStaffMember>> GetStaffMemberList(int accountInfoId);
        Task<List<RethinkStaffMembersByPermissionResponse>> GetStaffMemberListByPermission(int accountInfoId, List<string> permissions, string logicalOperator, int take = 10000, int skip = 0);
        Task<ProviderLocations> GetProviderLocation(int accountInfoId, int providerLocationId);
        Task<ProviderLocations> GetMainLocation(int accountInfoId);
        Task<ClientProviderLocationsModel> GetProviderLocationList(int accountInfoId);
        Task<List<ChildProfileRethinkModel>> GetChildProfileByName(int accountInfoId, string searchString);
        Task<List<ChildProfileRethinkModel>> GetChildProfileByFirstName(int accountInfoId, string searchString);
        Task<List<ClientTimezonesModel>> GetTimezones();
        Task<List<ServiceLines>> GetChildProfileFunderServiceLineMapping(int accountInfoId, int childProfileId);
        Task<ClientDiagnosisCodes> GetClientDiagnosisById(int accountInfoId, int clientId, int clientDiagnosisId);
        Task<FunderDetails> GetChildProfileFunderMappingByMappingId(int accountInfoId, int childProfileId, int mappingId);
        Task<ChildProfileEntityModel> GetChildProfileReturningEntity(int accountInfoId, int clientId);
        Task<AccountInfoEntityModel> GetAccountReturningEntityAsync(int accountInfoId, bool isChDetailsRequired = false);
        Task<DiagnosisEntityModel> GetClientDiagnosisByIdReturningEntityAsync(int clientDiagnosisId);
        Task<List<ChildProfileAuthorizationDiagnosisCode>> GetChildProfileAuthorizationDiagnosisCodesAsync(int accountInfoId, int childProfileId, int diagnosisId, int authId);
        Task<List<ClientDiagnosisModel>> GetClientDiagnosisListByDiagnosisIdAsync(int accountInfoId, int clientId, int clientDiagnosisId);
        Task<List<LocationCodesModel>> GetLocationCodes();
        Task<PlacesOfServiceModel> GetPlaceOfService(int accountInfoId);
        Task<Diagnosis> GetDiagnosisById(int diagnosisId);
        Task<RethinkAccountMembersListModel> GetMemberListAsync(int accountInfoId);
        Task<RethinkAccountMembersListModel> GetMembersAsync(int accountInfoId, string memberIds);
        Task<FunderListModel> GetFunderList(int accountInfoId);
        Task<FunderListModel> GetFunderListByName(string funderName);
        Task<FunderListModel> GetFunderListByTaxId(string taxId);
        Task<List<ClientUserContact>> GetInsuranceContactByPolicy(string policyNo);
        Task<ChildProfileServiceLines> GetServiceLine(int accountInfoId, int servicelineMappingId);
        Task<ClientDiagnosisCodeForClaimWithoutAut> GetClientDiagnosisReturningModelAsync(int accountInfoId, int clientId);

        Task<RethinkClientDetails> GetClientDetails(int accountInfoId, int clientId);
        //create the method to get guarantor information
        Task<List<RethinkGuarantorDetails.ClientModel>> GetClientDetailsGuarantor(int accountInfoId);
        Task<InsuranceContactsModel> GetInsuranceContactsIds(int accountInfoId, int clientId);
        Task<InsuranceContacts> GetContactGuarantorDetails(int accountInfoId, int clientId);
        Task<InsuranceContactsTypeModel> GetInsuranceContactsType(int accountInfoId, int childProfileId, int contactId);
        Task<ChildProfileFunderResponseModel> GetChildProfileFunderMappings(int accountInfoId, int childProfileId);
        Task<string> GetProviderLocationName(int accountInfoId, int childProfileId);
        Task<int> GetClearingHouseId(int accountInfoId);
        Task<ServiceLines> GetChildProfileFunderServiceLineMappingEntity(int accountInfoId, int childProfileId, int childProfileFunderMappingId, int servicelineMappingId);
        Task<InsuranceContacts> GetInsuranceContactEntity(int accountInfoId, int childProfileId, int insuranceContactId);
        Task<clientReferringProviders> GetChildProfileReferringProviderEntity(int accountInfoId, int childProfileId, int referringProviderId);
        Task<List<ServiceLines>> GetServiceLineMappingsByFunderId(int accountInfoId, int childProfileId, int childProfileFunderMappingId);
        Task<List<ServiceFunderData>> GetServiceFundersEntityListByFunderId(int accountInfoId, int childProfileId, int funderId);
        Task<ServiceFunderData> GetServiceFundersEntityById(int accountInfoId, int serviceFunderId);
        Task<List<StateModel>> GetStateList();
        Task<List<CountryModel>> GetCountryList();
        Task<List<ReferringProviderDropdownModel>> GetReferringProvidersByClientId(int clientId, int accountInfoId);
        Task<ProviderBillingCodeCredentialModel> GetProviderBillingCodeCredential(int accountInfoId, int providerBillingCode, int providerBillingCodeCredential);
        Task<List<AppointmentRethinkModel>> GetAppointmentListAsync(List<int> appoitmentIds);
        Task<AppointmentRethinkModel> GetAppointmentAsync(int appointmentId);
        Task<List<ClientAuthorizationBillingCodeModel>> GetClientAuthBillingCodesByAuthId(int accountInfoId, int childProfileId, int authId);
        Task<AppointmentClientAuthBillingCodeModel> GetChildProfileAuthBillingCodeForAppointment(int accountInfoId, int childProfileId, int billingCodleId);
        Task<PropagatingStaffMember> GetPropagatingStaffMemberById(int propStaffId);
        Task<AppointmentWorkFlowHistoyModel> GetWorkFlowHistoyDetailsById(int workFlowHistoryId);
        Task<ClearingHouseModel> GetClearingHouseDetails();
        Task<List<AppointmentRethinkModel>> GetCompletedAppointmentListAsync(int accountInfoId, int clientId, DateTime startDate);
        Task<ServiceLines> GetChildProfileFunderServiceLineMappingDataByClient(int accountInfoId, int childProfileId, int funderId, int serviceId);
        Task<ProviderLocationModel> GetChildProfileFacility(int accountInfoId, int childProfileId);
        Task<List<AuthRenderingProviderType>> GetRenderingProvidersAsync(int accountInfoId, bool withNpi = false);
        Task<ClientListUserModel> GetAllRenderingProvidersAsync(int accountInfoId);
        Task<PayerDetailsModel> GetPayerDetails(int funderId);
        Task<StateModel> GetStateById(int stateId);
        Task<CountryModel> GetCountryById(int countryId);
        Task<List<EraLocationCheckModel>> GetAccountByName(string name);
        Task<List<EraLocationCheckModel>> GetAccountInfoByTaxIDNPI(string taxId, string NPI);
        Task<List<FunderModel>> GetFunderInfoByTaxID(int accountInfoId, string taxId);
        Task<ClientDiagnosisCodeForClaimWithoutAut> GetClientDiagnosisByServiceId(int accountInfoId, int clientId, int serviceLineId);
        Task<ClientBillingCodesModel> GetBillingCodeList(int accountId);
        Task<List<AccountModel>> GetBillingAccountsAsync();
        Task<List<ChildProfileEntityModel>> GetChildProfilesForAccount(int accountInfoId);
        Task<List<int>> GetAllCompletedAppointmentsForAnAccountAsync(int accountInfoId, DateTime fromDate, DateTime toDate, int AppointmentTypeId=1);
    }
}
