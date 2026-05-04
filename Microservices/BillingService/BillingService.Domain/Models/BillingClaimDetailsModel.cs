using System;

namespace BillingService.Domain.Models
{
    public class BillingClaimDetailsModel
    {
        public int Id { get; set; }
        public int AssociatedAppointmentsCount { get; set; }
        public DateTime DOS { get; set; }
        public string BillingCode { get; set; }
        public string Modifiers { get; set; }
        public string Modifier1 { get; set; }
        public bool? IncludeOnClaimMod1 { get; set; }
        public string Modifier2 { get; set; }
        public bool? IncludeOnClaimMod2 { get; set; }
        public string Modifier3 { get; set; }
        public bool? IncludeOnClaimMod3 { get; set; }
        public string Modifier4 { get; set; }
        public bool? IncludeOnClaimMod4 { get; set; }
        public string Diagnosis { get; set; }
        public double Hours { get; set; }
        public decimal Units { get; set; }
        public decimal PerUnitsCharge { get; set; }
        public int UnitTypeValue { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public decimal PatientAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int UnitTypeId { get; set; }
        public string[] ReasonCodes { get; set; } = Array.Empty<string>();

        //note part
        public string? NoteText { get; set; }
        public int? NoteCreatedBy { get; set; }
        public string NoteCreatorName { get; set; }
        public DateTime? NoteCreatedDate { get; set; }

        public decimal TotalCount { get; set; }
        public string RenderingProvider { get; set; }
        public int? RenderingProviderId { get; set; }

    }
}
