namespace Rethink.Services.Common.Models.Claim
{
    public class AutoClaimRequestModel
    {
        public int appointmentId { get; set; }
        public int accountId { get; set; }

        public bool? processingSchedule { get; set; }
    }
}
