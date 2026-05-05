using System.ComponentModel;

namespace Rethink.Services.Common.Enums.BH
{
    public enum RethinkBillingStatus
    {
        [Description("Pending")]
        Pending = 1,
        [Description("Billed")]
        Billed = 2,
        [Description("Not Billed")]
        NotBilled = 3
    }
}