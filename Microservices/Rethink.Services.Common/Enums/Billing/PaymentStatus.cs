namespace Rethink.Services.Common.Enums.Billing
{
    public enum PaymentStatus
    {
        SubmittedForParsing = 0,
        ParsingError = 1,
        Unapplied = 2,
        PartiallyApplied = 3,
        FullyApplied = 4
    }
}