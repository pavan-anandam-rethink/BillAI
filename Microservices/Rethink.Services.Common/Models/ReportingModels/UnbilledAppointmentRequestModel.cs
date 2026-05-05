using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class UnbilledAppointmentsRequestModel : BaseAppointmentsRequestModel
    {
        public List<AppointmentRethinkModel>? AppointmentList { get; set; }
    }

    public class UnprocessedAppointmentsRequestModel : BaseAppointmentsRequestModel
    {
        // AppointmentList NOT included
    }


    public class BaseAppointmentsRequestModel
    {
        public List<int>? PayerOrFunder { get; set; }
        public List<int>? Clients { get; set; }
        public List<int>? Staff { get; set; }
        public List<int>? Location { get; set; }
        public List<int>? PlaceOfService { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<SortingModel> SortingModels { get; set; }
    }
}
