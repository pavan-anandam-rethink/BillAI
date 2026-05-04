using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class FunderFinancialSummaryResponse
    {
        public decimal StartingAR { get; set; }
        public string DateBasis { get; set; }
        public List<FunderFinancialRow> Rows { get; set; } = new();
        public FunderFinancialRow Total { get; set; } = new();
        public UnappliedCreditsDto UnappliedCredits { get; set; }
    }

    public class FunderFinancialRow
    {
        public int? FunderId { get; set; }
        public string FunderName { get; set; }
        public decimal PriorPeriodBalance { get; set; }
        public decimal Charges { get; set; }
        public decimal InsurancePay { get; set; }
        public decimal PatientPay { get; set; }
        public decimal TotalPay { get; set; }
        public decimal Adjustments { get; set; }
        public decimal WriteOffs { get; set; }
        public decimal PeriodBalance { get; set; }
        public decimal TotalBalance { get; set; }
    }
}
