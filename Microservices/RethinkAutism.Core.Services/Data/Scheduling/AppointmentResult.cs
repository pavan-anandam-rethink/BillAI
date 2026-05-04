using System.Collections.Generic;
using RethinkAutism.Contracts.DataObjects.Billing;
using RethinkAutism.Core.Services.Model;

namespace RethinkAutism.Core.Services.Data.Scheduling
{
    public class AppointmentResult
    {
        public List<Appointment> Appointments { get; set; }
        public Dictionary<int, List<BillingCodeDetail>> BillingCodeDetails { get; set; }
        public List<PreventableDate> PreventableDates { get; set; }
    }

    public class AppointmentResultShort
    {
        public List<AppointmentShort> Appointments { get; set; }
        public Dictionary<int, List<BillingCodeDetail>> BillingCodeDetails { get; set; }
    }
}
