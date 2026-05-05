using Rethink.Services.Common.Enums.Billing;
using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimModel
    {
        public int Id { get; set; }

        public string ClaimIdentifier { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int ChildProfileId { get; set; }

        public string ChildName { get; set; }

        public int MemberId { get; set; }

        public string MemberName { get; set; }

        public string ModifiedByMemberName { get; set; }

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

        public ModifyAppointmentsPermission ForbidAddAppointment { get; set; }

        public bool BilledPreviously { get; set; }

        public ClaimStatus StatusFrom { get; set; }
        public ClaimStatus Status { get; set; }
        public string StatusName { get; set; }

        public string Note { get; set; }

        public bool HasAppointmentLinks { get; set; }

        public decimal TotalCharges { get; set; }

        public bool? IsAppointmentDeleted { get; set; }

        public DateTime DateLastModified { get; set; }

        public decimal? PaidAmount { get; set; }
        public bool IsFlagged { get; set; }
        public bool IsManual { get; set; }

        public ClaimItem GetClaimItem()
        {
            ClaimItem item = new ClaimItem();

            // fields to update
            item.Id = Id;
            item.StartDate = StartDate;
            item.EndDate = EndDate;
            item.ChildProfileId = ChildProfileId;
            item.MemberId = MemberId;
            item.LocationCodeId = LocationCodeId;
            item.AuthorizationId = AuthorizationId;
            item.AuthorizationNumber = AuthorizationNumber;
            item.LocationId = LocationId;
            item.PrimaryFunderId = PrimaryFunderId;
            item.BillTo = BillTo;
            item.LastBilledFunderId = LastBilledFunderId;
            item.Status = Status;
            item.IsAppointmentDeleted = IsAppointmentDeleted;

            return item;
        }
    }
}