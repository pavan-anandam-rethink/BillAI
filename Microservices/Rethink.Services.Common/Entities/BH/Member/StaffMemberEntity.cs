using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class StaffMemberEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("tblMemberId")]
        public int MemberId { get; set; }
        public string CertificatName { get; set; }
        public string CertificatNumber { get; set; }
        public int? EmployeeTypeId { get; set; }
        public int? TitleTypeId { get; set; }
        public string NpiNumber { get; set; }
        public int? SupervisorId { get; set; }
        public bool IsActive { get; set; }
        [Column("hcStaffCertificationId")]
        public int? StaffCertificationId { get; set; }
        public int? GenderTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public bool? CanHanddleAgression { get; set; }
        public int? NumberOfClients { get; set; }
        public double? HourlyRate { get; set; }
        public double? AuthorizedHourPerWeek { get; set; }
        public double? BillableTargetPercent { get; set; }
        public double? BillableHoursTarget { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? MonthExperience { get; set; }
        public int AddressId { get; set; }
        public decimal? Salary { get; set; }
        [Column("hcStaffExperienceTypeId")]
        public int? StaffExperienceTypeId { get; set; }
        [Column("hcTimezoneId")]
        public int? TimezoneId { get; set; }
        [Column("hcStaffStatusId")]
        public int? StaffStatusId { get; set; }
        public bool ShowRbt { get; set; }
        public bool? IsNonExempt { get; set; }
        public bool ShowScheduling { get; set; }
        public string CompanyStaffID { get; set; }
        public string TaxonomyCode { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public bool? ByPassNpiRequirements { get; set; }

        public virtual StaffTitleEntity StaffTitle { get; set; }
        //public virtual ICollection<AppointmentEntity> Appointments { get; set; }
        // //public virtual StaffCertificationLUEntity StaffCertification { get; set; }
        public virtual MemberEntity Member { get; set; }
        public virtual TimeZoneEntity TimeZone { get; set; }
        public virtual AddressEntity Address { get; set; }
        public virtual StaffStatusEntity StaffStatus { get; set; }

        //public virtual MemberEntity Supervisor { get; set; }
        // public virtual List<NoteEntity> Notes { get; set; }
        //public virtual List<StaffServiceEntity> StaffServices { get; set; }

        //public void UpdateEntity(StaffMemberEntity staffMember)
        //{
        //    CertificatNumber = staffMember.CertificatNumber;
        //    CertificatName = staffMember.CertificatName;
        //    NpiNumber = staffMember.NpiNumber;
        //    EmployeeTypeId = staffMember.EmployeeTypeId;
        //    TitleTypeId = staffMember.TitleTypeId;
        //    SupervisorId = staffMember.SupervisorId;
        //    ModifiedBy = staffMember.ModifiedBy;
        //    DateLastModified = staffMember.DateLastModified;
        //    StaffCertificationId = staffMember.StaffCertificationId;
        //    Address.City = staffMember.Address.City;
        //    Address.StateId = staffMember.Address.StateId;
        //    Address.Street1 = staffMember.Address.Street1;
        //    Address.Zip = staffMember.Address.Zip;
        //    Address.Town = staffMember.Address.Town;
        //    Address.CountryId = staffMember.Address.CountryId;
        //    IsActive = staffMember.IsActive;
        //    StaffStatusId = staffMember.StaffStatusId;
        //    Salary = staffMember.Salary;
        //    ShowRbt = staffMember.ShowRbt;
        //    IsNonExempt = staffMember.IsNonExempt;
        //    TimezoneId = staffMember.TimezoneId;
        //    ShowScheduling = staffMember.ShowScheduling;
        //    CompanyStaffID = staffMember.CompanyStaffID;
        //    TaxonomyCode = staffMember.TaxonomyCode;
        //    DateOfBirth = staffMember.DateOfBirth;
        //}

        //public void UpdateAdditional(StaffMemberEntity staffMember)
        //{
        //    CanHanddleAgression = staffMember.CanHanddleAgression;
        //    BillableHoursTarget = staffMember.BillableHoursTarget;
        //    BillableTargetPercent = staffMember.BillableTargetPercent;
        //    DateOfBirth = staffMember.DateOfBirth;
        //    HourlyRate = staffMember.HourlyRate;
        //    AuthorizedHourPerWeek = staffMember.AuthorizedHourPerWeek;
        //    GenderTypeId = staffMember.GenderTypeId;
        //    StaffExperienceTypeId = staffMember.StaffExperienceTypeId;
        //    MemberId = staffMember.MemberId;
        //    MonthExperience = staffMember.MonthExperience;
        //    NumberOfClients = staffMember.NumberOfClients;
        //    StartDate = staffMember.StartDate;
        //}
    }
}