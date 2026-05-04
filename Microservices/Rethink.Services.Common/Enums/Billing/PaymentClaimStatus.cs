namespace Rethink.Services.Common.Enums.Billing
{
    public enum PaymentClaimStatus
    {
        // NOTE: these are actually stored as string in the incoming ERA
        Unknown = 0,
        ProcessedAsPrimary = 1,
        ProcessedAsSecondary = 2,
        ProcessedAsTertiery = 3,
        Processed = 123, // This is for 1 ,2 , 3 combined,
        Denied = 4,
        Reversal = 22,
        NotOurClaim = 23,
        NoPayment = 25,
    }
}