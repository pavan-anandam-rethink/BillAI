using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.ChildProfile;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Service;
using Rethink.Services.Common.Entities.Billing.Payment;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class ChildProfileEntity : BasePersistEntity, IAuditedEntity
    {
        public DateTime? DateOfBirth { get; set; }
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
        public string City { get; set; }
        public int? StateId { get; set; }
        public string ZipCode { get; set; }
        public int? CountryId { get; set; }
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


        public virtual AccountInfoEntity AccountInfo { get; set; }
        // //public virtual List<ChildProfileStaffMemberEntity> ChildProfileStaffMembers { get; set; }
        public virtual List<ChildProfileContactEntity> ChildProfileContacts { get; set; }
        //
        // //private string childFullName;
        // //[NotMapped]
        // //public string ChildFullName
        // //{
        // //    set
        // //    {
        // //        childFullName = FirstName + " " + LastName;
        // //    }
        // //    get
        // //    {
        // //        return FirstName + " " + LastName;
        // //    }
        // //}
        // //[NotMapped]
        // //public bool IsActive { get; set; }
        // //[NotMapped]
        // //public bool IsDemoClient { get; set; }
        // //[NotMapped]
        // //public string StatusName { get; set; }
        public virtual ProviderLocationEntity ProviderLocation { get; set; }
        public virtual StateEntity StateLU { get; set; }
        public virtual CountryEntity CountryLU { get; set; }
        // //public virtual GradeEntity Grade { get; set; }
        // //public virtual MemberEntity Member { get; set; }

        // From billing
        public virtual ICollection<PaymentClaimEntity> PaymentClaims { get; set; }

        
        
    }
}