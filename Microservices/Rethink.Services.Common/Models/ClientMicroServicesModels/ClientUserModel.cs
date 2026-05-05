using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientUserModel
    {
        public MetaData? metaData { get; set; }
        public int id { get; set; }
        public int accountId { get; set; }
        public DateTime dateOfBirth { get; set; }
        public int timezoneId { get; set; }
        public int? languageId { get; set; }
        public int memberId { get; set; }
        public int? genderId { get; set; }
        public string userType { get; set; }
        public List<ClientUserContact>? contacts { get; set; }
        public ClientUserName name { get; set; }
        public ClientAddress address { get; set; }
        public List<Identifiers> identifiers { get; set; }
        public List<Options> options { get; set; }
        public List<AttributeModel> attributes { get; set; }
        public bool areInteractionsLogging { get; set; }
        public bool isLockedOut { get; set; }
        public bool isApproved { get; set; }

    }


    [Owned]
    public class Identifiers
    {
        public int id { get; set; }
        public string identifierType { get; set; }
        public string value { get; set; }
    }
    [Owned]
    public class Options
    {
        public int id { get; set; }
        public int userOptionTypeId { get; set; }
        public string value { get; set; }
        public MetaData metaData { get; set; }

    }
    [Owned]
    public class Attributes
    {
        [Key]
        public int id { get; set; }
        public string type { get; set; }
        public string value { get; set; }
    }

    [Owned]
    public class AttributeModel
    {
        [Key]
        public string type { get; set; }
        public string value { get; set; }
    }

    [Owned]

    public class ChildProfileFunderMappingResponseModel
    {
        public int id { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string PatientName { get; set; }
        public string Age { get; set; }
        public int Uci { get; set; }
        public int ServiceIntensity { get; set; }
        public int PrimaryPolicy { get; set; }
        public int SecondaryPolicy { get; set; }
        public int Location { get; set; }

    }
    [Owned]
    public class ChildProfileEntityModel
    {
        public int Id { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int? NumberOfSiblings { get; set; }
        //not null in db!
        public int? GenderId { get; set; }
        public int? VerbalStatusId { get; set; }
        public int? AreaOfConcernId { get; set; }
        public string DiagnosisOtherText { get; set; }
        public bool FamilyRelatedDisorder { get; set; }
        public string FamilyRelatedDisorderText { get; set; }
        public string CaregiverOtherText { get; set; }
        public bool ShowRecommendedLessons { get; set; }
        public string HollowPhotoLocation { get; set; }
        public int? HollowSubscriberId { get; set; }
        public int? SubscriptionId { get; set; }
        public int? AssessmentStatusId { get; set; }
        public int? AgeGroupId { get; set; }
        public int? GradeId { get; set; }
        public int AccountInfoId { get; set; }
        public string PhotoLocation { get; set; }
        public int ProgramType { get; set; }
        public int? InclusionAssessmentStatusId { get; set; }
        public int? AdditionalHours { get; set; }
        public bool? SampleInd { get; set; }
        public bool? Archived { get; set; }
        public int? MemberId { get; set; }
        //not null db
        public string ActivityCenterAvatar { get; set; }
        public bool ActivityCenterTimer { get; set; }
        public bool ActivityCenterAudio { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StateTestNo { get; set; }
        public bool? HasAllocatedCoachingHours { get; set; }
        public int? LifeSkillsAssessmentStatusId { get; set; }
        public string MiddleName { get; set; }
        public int? StatusId { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; } = string.Empty;
        public int? StateId { get; set; }
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; }
        public int? CountryId { get; set; }
        public string Country { get; set; } = string.Empty;
        public string ReasonForReferral { get; set; }
        public bool? HipaaNopAgreement { get; set; }
        public DateTime? HipaaNopAgreementDate { get; set; }
        public string ClientId { get; set; }
        public int? FacilityId { get; set; }
        public string HealthConsiderations { get; set; }
        public Guid? Guid { get; set; }
        public int? TeamMemberId { get; set; }
        public string Notes { get; set; }
        public bool ShowClinical { get; set; }
        public bool ShowScheduling { get; set; }
        public bool ShowBilling { get; set; }
        public int ServiceIntensityTypeId { get; set; }

        public int? KareoPatientId { get; set; }
        public int? ClientOrder { get; set; }
        public string UCI { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Town { get; set; }

        public bool? IsNeedMasteryDecision { get; set; }
        public DateTime? MasteryDecisionRequested { get; set; }
        public DateTime? MasteryDecisionLastChecked { get; set; }
        public bool? IsMasteryDirty { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public virtual List<ChildProfileContactEntityModel> ChildProfileContacts { get; set; }
        public virtual AccountInfoEntityModel AccountInfo { get; set; }
        public virtual StateModel StateLU { get; set; }
        public virtual CountryModel CountryLU { get; set; }
    }
}