using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class AccountsReceivablesChargeLevelResponse
    {
        public int Id { get; set; }
        public string FunderName { get; set; }
        public int? ClientId { get; set; }
        public string ClientFirstName { get; set; }
        public string ClientLastName { get; set; }
        public string AppointmentId { get; set; } 
        public string BillingProvider { get; set; }
        public string RenderingProvider { get; set; }
        public string BillingCode { get; set; }
        public DateTime DateOfService { get; set; }
        public DateTime? BilledDate { get; set; }
        public int AgeInDays { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? AllowedAmount { get; set; }
        public decimal? BilledAmount { get; set; }
        public decimal Adjustments { get; set; }
        public decimal? AdjustedChargeAmount { get; set; } 
        public decimal? PatientPayments { get; set; } 
        public decimal PaymentsReceived { get; set; } 
        public decimal? NetReceivable { get; set; }
        public decimal OneToThirty { get; set; }
        public decimal ThirtyOneToSixty { get; set; }
        public decimal SixtyOneToNinty { get; set; }
        public decimal NintyOneToOneHundredTwenty { get; set; }
        public decimal MoreThanOneHundredTwenty { get; set; }
    }
}
