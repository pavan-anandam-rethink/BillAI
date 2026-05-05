using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimItem
    {
        public int Id { get; set; }

        public string ClaimIdentifier { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int AccountInfoId { get; set; }

        public int ChildProfileId { get; set; }

        public string ChildName { get; set; }

        public int MemberId { get; set; }

        public string MemberName { get; set; }

        public int LocationCodeId { get; set; }

        public string LocationCode { get; set; }

        public int? AuthorizationId { get; set; }

        public string AuthorizationNumber { get; set; }

        public int? LocationId { get; set; }

        public string LocationName { get; set; }

        public int PrimaryFunderId { get; set; }

        public int? BillTo { get; set; }

        public string PrimaryFunder { get; set; }

        public int? LastBilledFunderId { get; set; }

        public string CurrentFunder { get; set; }

        public ClaimStatus Status { get; set; }
        public ClaimSubmissionStatus SubmissionStatus { get; set; } = ClaimSubmissionStatus.Unknown;
        public string StatusName { get; set; }

        public string Note { get; set; }

        public bool HasAppointmentLinks { get; set; }

        public decimal TotalCharges { get; set; }

        public bool? IsAppointmentDeleted { get; set; }

        public DateTime DateLastModified { get; set; }

        public decimal? PaidAmount { get; set; }

        public string ModifiedByMemberName { get; set; }

        public ModifyAppointmentsPermission ForbidAddAppointment { get; set; } = ModifyAppointmentsPermission.Allow;

        public bool BilledPreviously { get; set; }
        public bool IsFlagged { get; set; }
        public bool IsManual { get; set; } = false;

        public DateTime? billedDate { get; set; }
        public void UpdateEntity(ClaimEntity entity)
        {
            // update data
            //entity.StartDate = StartDate; //not editable
            //entity.EndDate = EndDate; //not editable
            entity.ChildProfileId = ChildProfileId;
            entity.MemberId = MemberId;
            entity.LocationCodeId = LocationCodeId;
            entity.AuthorizationId = AuthorizationId;
            entity.AuthorizationNumber = AuthorizationNumber;
            entity.ToLocationId = LocationId;
            entity.ToLocation = LocationName;
            entity.BillTo = BillTo;
            entity.ClaimStatus = Status;
            entity.billedDate = billedDate;
        }
    }
}