using System.ComponentModel;

namespace RethinkAutism.Contracts.Enums.Curriculum
{
    public enum AppointmentStatus
    {
        [Description("Scheduled")]
        Scheduled = 1,
        [Description("Needs Verification")]
        NeedsVerification = 2,
        [Description("Completed")]
        Completed = 3,
        [Description("Cancelled Rescheduled")]
        CancelledReschedule = 4,
        [Description("Cancelled")]
        Cancelled = 5,
        [Description("Archived")]
        AutoCancelled = 6,
        [Description("Needs to Re-Verification")]
        NeedsReVerification = 7
    }
}