using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class MonthlyFinancialSummaryResponse
    {
        public decimal StartingAR { get; set; }
        public string DateBasis { get; set; }
        public List<MonthlyFinancialRow> Rows { get; set; } = new();
        public MonthlyFinancialRow Total { get; set; } = new();
        public UnappliedCreditsDto UnappliedCredits { get; set; }
    }

    public class MonthlyFinancialRow
    {
        public string MonthYear { get; set; }
        public decimal Charges { get; set; }
        public decimal InsurancePay { get; set; }
        public decimal PatientPay { get; set; }
        public decimal TotalPay { get; set; }
        public decimal Adjustments { get; set; }
        public decimal WriteOffs { get; set; }
        public decimal PeriodBalance { get; set; }
        public decimal EndingAR { get; set; }
    }

    public class UnappliedCreditsDto
    {
        public decimal InsuranceUnapplied { get; set; }
        public decimal PatientUnapplied { get; set; }
        public decimal TotalUnapplied { get; set; }
    }

}
