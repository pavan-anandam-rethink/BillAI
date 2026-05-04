using System.ComponentModel;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimSubmissionStatus
    {
        Unknown = 0,
        [Description("Pending submission to Clearinghouse")]
        ClearingHousePending = 1,
        [Description("Error submitting to Clearinghouse")]
        ClearingHouseError = 2,
        [Description("In Processing at Clearinghouse")]
        ClearinghouseProcessing = 3,
        [Description("Rejected by Clearinghouse")]
        ClearinghouseRejected = 4,
        [Description("Rejected by Funder")]
        FunderRejected = 5,
        [Description("Received by Funder")]
        FunderReceived = 6,
        [Description("Accepted by Funder")]
        FunderAccepted = 7,
        [Description("Submitted to Funder")]
        FunderSubmitted = 8,
        [Description("Funder Pending")]
        FunderPending = 9,
        [Description("Funder Denied")]
        FunderDenied = 10,
        [Description("Funder Processing Complete")]
        FunderProcessed = 11,
        [Description("Accepted by Clearinghouse")]
        ClearinghouseAccepted = 12,
        [Description("Abandoned")]
        Abandoned = 99, // re-submitted/corrected/voided

    }
}