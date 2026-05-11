namespace ClearingHouse.SharedKernel.Observability;

public static class TelemetryConstants
{
    public const string ServiceName = "ClearingHouseService";
    
    public static class ActivitySources
    {
        public const string SftpIngestion = "ClearingHouse.SftpIngestion";
        public const string BlobManagement = "ClearingHouse.BlobManagement";
        public const string FileMetadata = "ClearingHouse.FileMetadata";
        public const string BatchOrchestration = "ClearingHouse.BatchOrchestration";
        public const string EdiProcessing = "ClearingHouse.EdiProcessing";
        public const string Reconciliation = "ClearingHouse.Reconciliation";
        public const string FileTracking = "ClearingHouse.FileTracking";
        public const string Notification = "ClearingHouse.Notification";
        public const string StediIntegration = "ClearingHouse.StediIntegration";
    }
    
    public static class Metrics
    {
        public const string FilesIngested = "clearinghouse.files.ingested";
        public const string FilesProcessed = "clearinghouse.files.processed";
        public const string FilesFailed = "clearinghouse.files.failed";
        public const string BatchesCreated = "clearinghouse.batches.created";
        public const string BatchesCompleted = "clearinghouse.batches.completed";
        public const string ClaimsReconciled = "clearinghouse.claims.reconciled";
        public const string ProcessingDuration = "clearinghouse.processing.duration";
        public const string SftpDownloadDuration = "clearinghouse.sftp.download.duration";
        public const string EdiParsingDuration = "clearinghouse.edi.parsing.duration";
    }
}
