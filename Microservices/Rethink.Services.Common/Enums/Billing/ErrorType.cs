namespace Rethink.Services.Common.Enums.Billing
{
    public enum ErrorType
    {
        None = 0,

        // SFTP / Infrastructure Errors
        AuthFailure = 1,
        ConnectionFailure = 2,
        Timeout = 3,

        // Upload / Processing Errors
        UploadFailed = 4,
        FileGenerationFailed = 5,

        // Data / Validation Errors
        InvalidClearingHouseConfig = 6,
        ClaimNotFound = 7,
        ValidationFailed = 8,

        // System Errors
        Unknown = 9
    }
}
