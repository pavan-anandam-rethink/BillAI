using System;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;

namespace RethinkAutism.Data.Entities.Members
{
    public class SubscriptionEntity : BasePersistEntity, IAuditedEntity
    {
        public int Id { get; set; }
        public int? HollowSubscriberId { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime? NextPaymentDate { get; set; }
        public DateTime? PaidThroughDate { get; set; }
        public int SubscriptionStatusId { get; set; }
        public int? HollowTotalProfiles { get; set; }
        public int PaymentFrequencyId { get; set; }
        public decimal AmountPerBilling { get; set; }
        public int SubscriptionTypeId { get; set; }
        public int? PaymentMethodId { get; set; }
        public int? SupportTypeId { get; set; }
        public bool AutomatedBilling { get; set; }
        public bool? Termsacceptance { get; set; }
        public bool? CustomizedCurriculum { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public int? SubscriptionSubTypeId { get; set; }
        public DateTime? LastNotificationSent { get; set; }
        public DateTime LastPaymentDate { get; set; }
        public decimal LastBillingAmount { get; set; }
        public bool? CommonStandards { get; set; }
        public bool? BehaviorTracking { get; set; }
        public int? MaxAccountProfiles { get; set; }
        public bool? Iep { get; set; }
        public int AccountInfoId { get; set; }
        public DateTime DateSubscriptionStatusChanged { get; set; }
        public bool? EzData { get; set; }
        public bool? MasteryCriteria { get; set; }
        public bool Inclusion { get; set; }
        public bool EmployeeBenefitProgram { get; set; }
        public bool? EmployeeBenefitProvider { get; set; }
        public bool ExtendSessionTimeout { get; set; }
        public int OrganizationTypeId { get; set; }
        public bool? PreTestTarget { get; set; }
        public int? CoachingTypeId { get; set; }
        public bool? TrainingProgram { get; set; }
        public int? MaxAccountMembers { get; set; }
        //public int? SubscriptionModuleTypeId { get; set; }
        public bool ShowRbt { get; set; }
        public bool ShowSetss { get; set; }
        public bool? AcademicCurriculum { get; set; }
        //public int? MaxAccountTcMembers { get; set; }
        //public bool TrainingCenter { get; set; }
        //public bool BasicTraining { get; set; }
        //public int? TrainingProgramDefaultRole { get; set; }
        public int VbMappLicenses { get; set; }
        public bool ShowVbMapp { get; set; }
        //public bool ShowFoundation { get; set; }
        //public bool ShowDisability { get; set; }
        public bool ShowClinical { get; set; }
        public bool ShowScheduling { get; set; }
        public bool ShowBilling { get; set; }
        public bool ShowANSIClaims { get; set; }
        public int? BillingOptionId { get; set; }
        public bool DisableCKEditor { get; set; }
        public bool ShowParentVerification { get; set; }
        public bool ShowMessaging { get; set; }
        public bool ShowMedicalNecessity { get; set; }
        public bool AssessmentOnly { get; set; }
        public bool ShowNycInvoicing { get; set; }
        public int? MaxDemoClients { get; set; }
        public bool ShowGraphs { get; set; }
        public bool ShowTherawe { get; set; }
        public bool ShowSecuritySettings { get; set; }
        public bool ShowEVV { get; set; }
        public bool ShowDPH { get; set; }
        public Int16 IsNextGen { get; set; }
        public virtual AccountInfoEntity AccountInfo { get; set; }
        public virtual SubscriptionModuleLuEntity SubscriptionModuleType { get; set; }
    }
}
