using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class UpdateBillingClaimDetailsListModel
    {
        public List<UpdateBillingClaimDetailsModel> BillingClaimDetailsModels { get; set; }
        public int MemberId { get; set; }
    }

    public class UpdateBillingClaimDetailsModel
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public string Modifier1 { get; set; }
        public string Modifier2 { get; set; }
        public string Modifier3 { get; set; }
        public string Modifier4 { get; set; }
        public decimal Units { get; set; }
        public decimal PerUnitsCharge { get; set; }
        public int AccountId { get; set; }
        public string BillingCode { get; set; }
        public DateTime DateOfService { get; set; }
        public string Diagnosis { get; set; }
        public double Hours { get; set; }
        public string? NoteText { get; set; }
        public int? NoteCreatedBy { get; set; }
        public string NoteCreatorName { get; set; }
        public DateTime? NoteCreatedDate { get; set; }
        public decimal TotalCount { get; set; }
        public int RenderingProviderId { get; set; }
    }
}
