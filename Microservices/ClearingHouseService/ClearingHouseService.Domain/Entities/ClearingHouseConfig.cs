using ClearingHouseService.Domain.ValueObjects;

namespace ClearingHouseService.Domain.Entities
{
    /// <summary>
    /// Represents the configuration for a clearing house, including connection details and credentials.
    /// </summary>
    public class ClearingHouseConfig
    {
        public int ClearingHouseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public ClearingHouseType Type { get; set; } = ClearingHouseType.Stedi;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserPassword { get; set; } = string.Empty;
        public string UploadDirectory { get; set; } = string.Empty;
        public string DownloadDirectory { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? ApiBaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
