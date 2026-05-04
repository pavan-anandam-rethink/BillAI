namespace RethinkAutism.Core.Services.Data.Scheduling
{
    public class EVVReason
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public int EVVReasonCodeId { get; set; }

        public bool? IsDeactivated { get; set; }

        public string Note { get; set; }
    }
}
