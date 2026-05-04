using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class ClaimFollowUpReportModel
    {
        public int Id { get; set; }
        public string ClaimId { get; set; }
        public int ClaimIdValue { get; set; }
        public int MemberId { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int Status { get; set; }
        public int PaymentAdjustmentId { get; set; }
        public int ClientId { get; set; }
        public string ClientFirst { get; set; }
        public string ClientLast { get; set; }
        public string FunderName { get; set; }
        public string RenderingProvider { get; set; }
        public string PlaceOfService { get; set; }
        public DateTime? ClaimFrom { get; set; }
        public DateTime? ClaimThrough { get; set; }
        public string Authorization { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? BilledAmount { get; set; }
        public decimal? PaymentAmount { get; set; }
        public decimal? AdjustmentAmount { get; set; }
        public decimal? Balance { get; set; }
        public DateTime? BilledDate { get; set; }
        public string ClaimStatus { get; set; }
        public string Note { get; set; }
        public int CreatedBy { get; set; }
        public DateTime NoteCreatedDate { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public DateTime? DateOfService { get; set; }
        public DateTime PaymentDateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public string FollowUpStatus { get; set; }
        public string NoteCreatedByName { get; set; }
    }
}
