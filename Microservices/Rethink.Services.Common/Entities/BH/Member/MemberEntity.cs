using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.ChildProfile;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class MemberEntity : BasePersistEntity, IAuditedEntity
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PasswordQuestion { get; set; }
        public string PasswordAnswer { get; set; }
        public string Comment { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime? DateLastLoggedIn { get; set; }
        public DateTime? DateLastPasswordChanged { get; set; }
        public bool? IsOnLine { get; set; }
        public bool? IsLockedOut { get; set; }
        public DateTime? DateLastLockedOut { get; set; }
        public int? FailedPasswordAttemptCount { get; set; }
        public DateTime? FailedPasswordAttemptWindowStart { get; set; }
        public int? FailedPasswordAnswerAttemptCount { get; set; }
        public DateTime? FailedPasswordAnswerAttemptWindowStart { get; set; }


        public DateTime? DateLastActive { get; set; }


        public int AccountInfoId { get; set; }
        public string Title { get; set; }
        public string ScreenName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string MobileNumber { get; set; }
        public string SMSPhone { get; set; }
        public string HollowPhotoLocation { get; set; }
        public bool? TipOfTheDay { get; set; }
        public bool? GroupMessageEmail { get; set; }
        public string HearAboutUs { get; set; }
        public string Notes { get; set; }
        public string LogoFileName { get; set; }
        public string CollegeName { get; set; }
        public string CourseNoName { get; set; }
        public string InstructorName { get; set; }
        public string InstructorContact { get; set; }
        public int? ChildRelationShipId { get; set; }
        public string ChildRelationshipText { get; set; }
        public string PhotoLocation { get; set; }
        public int? EmpBenefitChildRelationShipId { get; set; }
        public string EmpBenefitChildRelationshipText { get; set; }
        public string GroupScreenname { get; set; }
        public bool? HideNewFeaturesReminder { get; set; }
        public bool? EmailPreference { get; set; }
        public bool? HideBehaviorManagementPopup { get; set; }
        public string TwoFactorVerificationCode { get; set; }
        public string MiddleName { get; set; }
        public Guid Guid { get; set; }
        public Guid? TempGuid { get; set; }
        public Guid? ApiKey { get; set; }
        public Guid? PasswordResetToken { get; set; }
        public DateTime? PasswordResetDate { get; set; }
        public bool EnableAccountAPI { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }


        public virtual AccountInfoEntity AccountInfo { get; set; }
        public virtual List<ChildProfileContactEntity> ChildProfileContacts { get; set; }
        public virtual List<PropagatingChildProfileAuthorizationEntity> PropagatingChildProfileAuthorizationEntities { get; set; }
        //public virtual MemberViewSettingEntity MemberViewSetting { get; set; }
        //public virtual ICollection<StaffMemberEntity> StaffMembers { get; set; }
        //public virtual List<StaffMemberLocationEntity> StaffMemberLocations { get; set; }
        //public virtual ICollection<MemberAccountRoleEntity> MemberAccountRoles { get; set; }
        //public virtual List<StaffMemberAgeGroupEntity> AgeGroups { get; set; }
        //public virtual List<StaffMemberLanguagesEntity> Languages { get; set; }
        //public virtual List<ChildProfileContactEntity> ChildProfileContacts { get; set; }


        //[NotMapped]
        //public string FullName
        //{
        //    get
        //    {
        //        return this.FirstName + " " + this.LastName;
        //    }
        //}

        //public virtual ICollection<MemberRoleEntity> MemberRoles { get; set; }
        //public virtual List<StaffMemberCredentialEntity> StaffMemberCredentials { get; set; }
    }
}