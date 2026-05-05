namespace RethinkAutism.Core.Services.Data.Scheduling
{
    public class AcknowledgeableException
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public int ExceptionId { get; set; }
    }
}
