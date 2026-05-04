using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.Clients;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientAuthorization
    {
        public int childProfileId { get; set; }
        public int funderId { get; set; }
        public int providerServiceId { get; set; }
        public int? authorizationTypeId { get; set; }
        public string authorizationNumber { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string attachment { get; set; }
        public string attachmentFileName { get; set; }
        public int authorizationDistributionTypeId { get; set; }
        public int childProfileDiagnosisId { get; set; }
        public int authorizationRenderingProviderTypeId { get; set; }
        public int? renderingProviderStaffId { get; set; }
        public int authorizationSubmissionTypeId { get; set; }
        public int? totalNumberOfUnits { get; set; }
        public int? childProfileReferringProviderId { get; set; }
        public List<int> childProfileFunderServiceLineMappingId { get; set; }
        //public int? serviceFacilityLocationId { get; set; }
        public int? billingProviderId { get; set; }
        public int id { get; set; }

        public DateTime? renderingProviderDateUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? referringProviderDateUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? billingProviderDateUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? serviceFacilityLocationDateUpdated { get; set; } = DateTime.UtcNow;
        public MetaData metaData { get; set; }
        [NotMapped]
        public FunderDataModel Funder { get; set; }
        [NotMapped]
        public ServiceLines ChildProfileFunderServiceLineMapping { get; set; }
        [NotMapped]
        public ProviderLocations ServiceFacilityLocation { get; set; }
        [NotMapped]
        public ProviderLocations BillingProvider { get; set; }
        [NotMapped]
        public List<ChildProfileAuthorizationDiagnosisCode> ChildProfileAuthorizationDiagnosisCodes { get; set; }
        [NotMapped]
        public clientReferringProviders ChildProfileReferringProvider { get; set; }
        [NotMapped]
        public ClientDiagnosisCodes ChildProfileDiagnosis { get; set; }
        [NotMapped]
        public RethinkAccountMember RenderingProvider { get; set; }
    }


    [Owned]
    public class AppointmentClientAuthBillingCodeModel
    {
        public int childProfileAuthorizationId { get; set; }
        public int? providerBillingCodeId { get; set; }
        public int noOfUnits { get; set; }
        public int unitTypeId { get; set; }
        public int frequencyTypeId { get; set; }
        public int schedulingGoalNoOfUnits { get; set; }
        public int schedulingGoalFrequencyTypeId { get; set; }
        public int providerServiceId { get; set; }
        public int id { get; set; }
        public BillingCodeModel ProviderBillingCode { get; set; }
        public ClientAuthorization ChildProfileAuthorization { get; set; }
        public BillingCodeData AppointmentProviderBillingCode { get; set; }
        public MetaData metaData { get; set; }
    }
}
