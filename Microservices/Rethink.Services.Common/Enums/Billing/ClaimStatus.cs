using System.ComponentModel;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimStatus
    {
        None = 0,
        [Description("Pending Review")]
        PendingReview = 1,
        Void = 2,
        Billed = 3,
        Pending = 4,
        Denied = 5,
        Closed = 6,
        [Description("Ready To Bill")]
        ReadyToBill = 7,
        [Description("Clearinghouse - Rejected")]
        RejectedClearinghouse = 8,
        [Description("Funder - Rejected")]
        RejectedFunder = 9,
        Rebill = 10,
        [Description("Bill Next Funder")]
        BillNextFunder = 11,
        Paid = 12,
        Success = 13,
        Fail = 14,
        [Description("Clearinghouse - Accepted")]
        AcceptedClearingHouse = 15,
        [Description("Funder - Accepted")]
        AcceptedFunder = 16,
        [Description("Funder - Received")]
        ReceivedFunder = 17,
        [Description("Void - Closed")]
        VoidClosed = 18,
        [Description("Submission Failed")]
        SubmissionFailed = 19,
        [Description("Approval Failed")]
        ApprovalFailed = 20
    }
}