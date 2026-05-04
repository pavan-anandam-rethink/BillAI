using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class AppointmentModel
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TimeRange { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string ClientName { get; set; }
        public string FunderName { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public string ServiceName { get; set; }
        public string ServiceLocation { get; set; }
        public string BillingCode { get; set; }
        public string BillingCode2 { get; set; }
        public string? appointmentErrorMessage { get; set; }
    }

    public class AppointmentModelWithCount
    {
        public List<AppointmentModel> appointmentModels { get; set; }
        public int totalCount { get; set; }
    }

    public class ExportExcelRequestModel : UserInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AppointmentModel> data { get; set; }
    }
}