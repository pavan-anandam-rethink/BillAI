namespace ClearingHouseService.Domain.Entities
{
    /// <summary>
    /// Represents the result of a transmission operation (send/receive) to/from a clearing house.
    /// </summary>
    public class TransmissionResult
    {
        public bool IsSuccess { get; set; }
        public string? FileName { get; set; }
        public string? ErrorMessage { get; set; }
        public TransmissionErrorType ErrorType { get; set; } = TransmissionErrorType.None;
        public long DurationMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static TransmissionResult Success(string fileName, long durationMs = 0)
        {
            return new TransmissionResult
            {
                IsSuccess = true,
                FileName = fileName,
                ErrorType = TransmissionErrorType.None,
                DurationMs = durationMs
            };
        }

        public static TransmissionResult Fail(TransmissionErrorType errorType, string message)
        {
            return new TransmissionResult
            {
                IsSuccess = false,
                ErrorType = errorType,
                ErrorMessage = message
            };
        }
    }

    /// <summary>
    /// Types of errors that can occur during clearing house transmission.
    /// </summary>
    public enum TransmissionErrorType
    {
        None,
        AuthenticationFailure,
        ConnectionFailure,
        Timeout,
        UploadFailed,
        DownloadFailed,
        FileGenerationFailed,
        InvalidConfiguration,
        ValidationFailed,
        Unknown
    }
}
