using System.ComponentModel;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimAction : byte
    {
        Create = 1,
        Edit = 2,
        Delete = 3,
        [Description("Scrubbing Rules")]
        ScrubbingRules = 4,
        Acknowledgment = 5,
        Reject = 6,
        Denied = 7,
        Billed = 8,
        Invoiced = 9,
        Printed = 10,
        Completed = 11,
        Pending = 12,
        RePrinted = 13,
        Flag = 14,
        Unflag = 15,
        Approve = 16,
        Submit = 17,
        Void = 18,
        Rebill = 19,
        Writeoff = 20,
        [Description("Bill Next Funder")]
        BillNextFunder = 21,
        [Description("Payment Applied")]
        PaymentApplied = 22,
        [Description("Payment Posted")]
        PaymentPosted = 23,
        [Description("Payment Removed")]
        PaymentRemoved = 24,
        [Description("Payment Received")]
        PaymentReceived = 25,
        Unapprove = 26,
        Added = 27,
        ClaimProcessing = 28,
        ClaimReconciled = 29,
        PatientResponsibility = 30,
        Adjustment = 31,
    }
}
