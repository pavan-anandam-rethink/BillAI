using System;

namespace RethinkAutism.Core.Services.Data.Curriculum
{
    public class ExceedAuthorizationHourEventModel
    {
        public int AppointmentId { get; set; }
        public DateTime EventStart { get; set; }
        public int? Minutes { get; set; }
    }
}
