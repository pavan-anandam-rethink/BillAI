using Rethink.Services.Common.Enums.Billing;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class ReportModelBase
    {
        public int AccountID { get; set; }
        public string AccountName { get; set; }
        public ReportFrequency ReportFrequency { get; set; }
    }

    public class ReportResponseModel : ReportModelBase
    {
        public int Count835 { get; set; }
        public int Count837 { get; set; }
        public decimal TotalBilled { get; set; } // Added property to fix CS0117  
        public int Total => Count835 + Count837;
        public bool OSB { get; set; }
        public string IntactId { get; set; }
    }

}
