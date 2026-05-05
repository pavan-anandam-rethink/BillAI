using System.ComponentModel;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimActionMode : byte
    {
        User = 1,
        System = 2
    }

    public enum ClaimFlagActionMode
    {
        [Description("Flagged")]
        Flagged = 1,
        [Description("Unflagged")]
        Unflagged = 2,
        [Description("Updated")]
        Updated
    }
}
