namespace ClearingHouse.SharedKernel.Observability;

public static class TelemetryConstants
{
    public const string ServiceName = "ClearingHouse.Platform";

    public static class Activities
    {
        public const string SftpPoll = "sftp.poll";
        public const string SftpDownload = "sftp.download";
        public const string SftpUpload = "sftp.upload";
        public const string BlobUpload = "blob.upload";
        public const string BlobDownload = "blob.download";
        public const string EdiParse = "edi.parse";
        public const string EdiProcess = "edi.process";
        public const string BatchProcess = "batch.process";
        public const string Reconcile = "reconcile.claim";
        public const string PublishEvent = "event.publish";
    }

    public static class Metrics
    {
        public const string FilesIngested = "clearinghouse.files.ingested";
        public const string FilesProcessed = "clearinghouse.files.processed";
        public const string FilesFailed = "clearinghouse.files.failed";
        public const string ProcessingDuration = "clearinghouse.processing.duration";
        public const string QueueDepth = "clearinghouse.queue.depth";
        public const string ActiveBatches = "clearinghouse.batches.active";
        public const string ReconciliationPending = "clearinghouse.reconciliation.pending";
        public const string SftpConnectionErrors = "clearinghouse.sftp.errors";
    }

    public static class Tags
    {
        public const string ClearinghouseId = "clearinghouse.id";
        public const string FileType = "file.type";
        public const string CorrelationId = "correlation.id";
        public const string BatchId = "batch.id";
        public const string Status = "status";
        public const string ErrorType = "error.type";
    }
}
