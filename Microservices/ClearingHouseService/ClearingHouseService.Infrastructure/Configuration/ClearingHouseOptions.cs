namespace ClearingHouseService.Infrastructure.Configuration
{
    /// <summary>
    /// Strongly-typed options for clearing house configuration.
    /// Bound from the "Clearinghouses" section in appsettings.json.
    /// </summary>
    public class ClearingHouseOptions
    {
        public const string SectionName = "Clearinghouses";

        public StediOptions Stedi { get; set; } = new();
        public AvailityOptions Availity { get; set; } = new();
    }

    /// <summary>
    /// Configuration options for the Stedi clearing house.
    /// </summary>
    public class StediOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string EligibilityUrl { get; set; } = string.Empty;
        public string GetPayersUrl { get; set; } = string.Empty;
        public string GetEnrollmentUrl { get; set; } = string.Empty;
        public string Hosts { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserPassword { get; set; } = string.Empty;
        public string UploadDirectory { get; set; } = string.Empty;
        public string DownloadDirectory { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration options for the Availity clearing house.
    /// </summary>
    public class AvailityOptions
    {
        public string Hosts { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserPassword { get; set; } = string.Empty;
        public string UploadDirectory { get; set; } = string.Empty;
        public string DownloadDirectory { get; set; } = string.Empty;
    }
}
