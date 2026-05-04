using System.ComponentModel;

namespace RethinkAutism.Contracts.Enums.Curriculum
{
    public enum AppointmentTypes
    {
        Billable = 1,

        [Description("Non-Billable")]
        NonBillable = 2,
        Travel = 3,
    }
}
