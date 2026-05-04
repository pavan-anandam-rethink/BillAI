using System;
using Rethink.Services.Common.Enums.Billing;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class ReportQueryModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ReportFrequency ReportFrequency { get; set; }
    }
}

