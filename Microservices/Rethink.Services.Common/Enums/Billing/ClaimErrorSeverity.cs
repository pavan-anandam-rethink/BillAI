namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimErrorSeverity
    {
        Unknown = 0,
        Fatal = 1,  // Unrecoverable error - stop processing
        Error = 2,  // Error
        Warning = 3,  // Warning or Alert about a potential problem
        NoError = 99, // Indicates that there are no errors (used for the rollup severity)
    }
}