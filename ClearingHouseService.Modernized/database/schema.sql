-- ============================================================================
-- ClearingHouse Modernized - Enterprise Database Schema
-- Healthcare EDI Processing Platform
-- ============================================================================

-- ============================================================================
-- SCHEMA: clearinghouse
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'clearinghouse')
    EXEC('CREATE SCHEMA clearinghouse')
GO

-- ============================================================================
-- TABLE: FilesRawLog
-- Purpose: Raw log of all ingested files from SFTP/API sources
-- ============================================================================
CREATE TABLE clearinghouse.FilesRawLog (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FileName NVARCHAR(500) NOT NULL,
    SourcePath NVARCHAR(2000),
    BlobUri NVARCHAR(2000),
    FileSizeBytes BIGINT NOT NULL DEFAULT 0,
    ContentHash NVARCHAR(128),
    ClearinghouseId INT NOT NULL,
    ClearinghouseName NVARCHAR(100) NOT NULL,
    TransactionType INT,
    Status INT NOT NULL DEFAULT 0,
    CorrelationId NVARCHAR(128) NOT NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(4000),
    IngestedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ProcessedAt DATETIME2,
    ArchivedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    CreatedBy NVARCHAR(200),
    UpdatedBy NVARCHAR(200),
    Version INT NOT NULL DEFAULT 1,
    INDEX IX_FilesRawLog_CorrelationId NONCLUSTERED (CorrelationId),
    INDEX IX_FilesRawLog_ClearinghouseId_Status NONCLUSTERED (ClearinghouseId, Status),
    INDEX IX_FilesRawLog_CreatedAt NONCLUSTERED (CreatedAt DESC),
    INDEX IX_FilesRawLog_TransactionType NONCLUSTERED (TransactionType) INCLUDE (Status, ClearinghouseId)
);
GO

-- ============================================================================
-- TABLE: ClearinghouseFiles
-- Purpose: Processed clearinghouse file records with full lifecycle tracking
-- ============================================================================
CREATE TABLE clearinghouse.ClearinghouseFiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FileRawLogId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    BlobUri NVARCHAR(2000) NOT NULL,
    ContainerName NVARCHAR(200) NOT NULL,
    FolderPath NVARCHAR(500),
    FileSizeBytes BIGINT NOT NULL,
    ContentHash NVARCHAR(128),
    ClearinghouseId INT NOT NULL,
    ClearinghouseName NVARCHAR(100) NOT NULL,
    TransactionType INT NOT NULL,
    Direction INT NOT NULL, -- 0=Inbound, 1=Outbound
    Status INT NOT NULL DEFAULT 0,
    CorrelationId NVARCHAR(128) NOT NULL,
    BatchId UNIQUEIDENTIFIER,
    AccountInfoId INT,
    RetryCount INT NOT NULL DEFAULT 0,
    MaxRetries INT NOT NULL DEFAULT 3,
    ErrorMessage NVARCHAR(4000),
    ErrorCode NVARCHAR(50),
    ProcessingStartedAt DATETIME2,
    ProcessingCompletedAt DATETIME2,
    ArchivedAt DATETIME2,
    ExpiresAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    CreatedBy NVARCHAR(200),
    UpdatedBy NVARCHAR(200),
    Version INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ClearinghouseFiles_FilesRawLog FOREIGN KEY (FileRawLogId) REFERENCES clearinghouse.FilesRawLog(Id),
    INDEX IX_ClearinghouseFiles_BatchId NONCLUSTERED (BatchId),
    INDEX IX_ClearinghouseFiles_CorrelationId NONCLUSTERED (CorrelationId),
    INDEX IX_ClearinghouseFiles_Status NONCLUSTERED (Status) INCLUDE (ClearinghouseId, TransactionType),
    INDEX IX_ClearinghouseFiles_ClearinghouseId_TransactionType NONCLUSTERED (ClearinghouseId, TransactionType, Status),
    INDEX IX_ClearinghouseFiles_CreatedAt NONCLUSTERED (CreatedAt DESC),
    INDEX IX_ClearinghouseFiles_AccountInfoId NONCLUSTERED (AccountInfoId)
);
GO

-- ============================================================================
-- TABLE: FileBatchHeader
-- Purpose: Batch processing header records
-- ============================================================================
CREATE TABLE clearinghouse.FileBatchHeader (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BatchName NVARCHAR(200),
    ClearinghouseId INT NOT NULL,
    ClearinghouseName NVARCHAR(100) NOT NULL,
    TransactionType INT,
    Status INT NOT NULL DEFAULT 0,
    Priority INT NOT NULL DEFAULT 0,
    TotalFiles INT NOT NULL DEFAULT 0,
    ProcessedFiles INT NOT NULL DEFAULT 0,
    FailedFiles INT NOT NULL DEFAULT 0,
    ConcurrencyLimit INT NOT NULL DEFAULT 5,
    CorrelationId NVARCHAR(128) NOT NULL,
    ScheduledAt DATETIME2,
    StartedAt DATETIME2,
    CompletedAt DATETIME2,
    TimeoutAt DATETIME2,
    ErrorMessage NVARCHAR(4000),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    CreatedBy NVARCHAR(200),
    Version INT NOT NULL DEFAULT 1,
    INDEX IX_FileBatchHeader_Status NONCLUSTERED (Status) INCLUDE (ClearinghouseId),
    INDEX IX_FileBatchHeader_CorrelationId NONCLUSTERED (CorrelationId),
    INDEX IX_FileBatchHeader_CreatedAt NONCLUSTERED (CreatedAt DESC),
    INDEX IX_FileBatchHeader_ClearinghouseId NONCLUSTERED (ClearinghouseId, Status)
);
GO

-- ============================================================================
-- TABLE: FileBatchItems
-- Purpose: Individual items within a batch
-- ============================================================================
CREATE TABLE clearinghouse.FileBatchItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BatchId UNIQUEIDENTIFIER NOT NULL,
    FileId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    BlobUri NVARCHAR(2000),
    SequenceNumber INT NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(4000),
    ProcessingStartedAt DATETIME2,
    ProcessingCompletedAt DATETIME2,
    WorkerNodeId NVARCHAR(200),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    Version INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_FileBatchItems_BatchHeader FOREIGN KEY (BatchId) REFERENCES clearinghouse.FileBatchHeader(Id),
    INDEX IX_FileBatchItems_BatchId NONCLUSTERED (BatchId) INCLUDE (Status),
    INDEX IX_FileBatchItems_FileId NONCLUSTERED (FileId),
    INDEX IX_FileBatchItems_Status NONCLUSTERED (Status)
);
GO

-- ============================================================================
-- TABLE: FileEventHistory
-- Purpose: Complete event audit trail for file lifecycle
-- ============================================================================
CREATE TABLE clearinghouse.FileEventHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FileId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    EventData NVARCHAR(MAX),
    PreviousStatus INT,
    NewStatus INT,
    CorrelationId NVARCHAR(128),
    TriggeredBy NVARCHAR(200),
    ServiceName NVARCHAR(200),
    NodeId NVARCHAR(200),
    ErrorMessage NVARCHAR(4000),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    INDEX IX_FileEventHistory_FileId NONCLUSTERED (FileId) INCLUDE (EventType, CreatedAt),
    INDEX IX_FileEventHistory_CorrelationId NONCLUSTERED (CorrelationId),
    INDEX IX_FileEventHistory_CreatedAt NONCLUSTERED (CreatedAt DESC),
    INDEX IX_FileEventHistory_EventType NONCLUSTERED (EventType) INCLUDE (FileId)
);
GO

-- ============================================================================
-- TABLE: ClaimReconciliation
-- Purpose: Claim matching and reconciliation records
-- ============================================================================
CREATE TABLE clearinghouse.ClaimReconciliation (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ClaimId NVARCHAR(100) NOT NULL,
    FileId UNIQUEIDENTIFIER,
    BatchId UNIQUEIDENTIFIER,
    ClearinghouseId INT NOT NULL,
    TransactionType INT NOT NULL,
    SubmissionStatus NVARCHAR(50),
    ResponseStatus NVARCHAR(50),
    AcknowledgmentCode NVARCHAR(10),
    RejectReasonCode NVARCHAR(50),
    RejectReasonDescription NVARCHAR(500),
    PaymentAmount DECIMAL(18,2),
    AdjustmentAmount DECIMAL(18,2),
    PatientResponsibilityAmount DECIMAL(18,2),
    PayerClaimControlNumber NVARCHAR(100),
    ClearinghouseTraceNumber NVARCHAR(100),
    CorrelationId NVARCHAR(128) NOT NULL,
    SubmittedAt DATETIME2,
    AcknowledgedAt DATETIME2,
    ReconciledAt DATETIME2,
    ErrorMessage NVARCHAR(4000),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    Version INT NOT NULL DEFAULT 1,
    INDEX IX_ClaimReconciliation_ClaimId NONCLUSTERED (ClaimId),
    INDEX IX_ClaimReconciliation_FileId NONCLUSTERED (FileId),
    INDEX IX_ClaimReconciliation_CorrelationId NONCLUSTERED (CorrelationId),
    INDEX IX_ClaimReconciliation_ClearinghouseId_Status NONCLUSTERED (ClearinghouseId, ResponseStatus),
    INDEX IX_ClaimReconciliation_CreatedAt NONCLUSTERED (CreatedAt DESC),
    INDEX IX_ClaimReconciliation_SubmissionStatus NONCLUSTERED (SubmissionStatus) INCLUDE (ClaimId, ClearinghouseId)
);
GO

-- ============================================================================
-- TABLE: PaymentReconciliation
-- Purpose: ERA/835 payment reconciliation
-- ============================================================================
CREATE TABLE clearinghouse.PaymentReconciliation (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ClaimReconciliationId UNIQUEIDENTIFIER,
    ClaimId NVARCHAR(100) NOT NULL,
    PayerName NVARCHAR(200),
    PayerId NVARCHAR(50),
    CheckNumber NVARCHAR(100),
    CheckDate DATETIME2,
    TotalClaimChargeAmount DECIMAL(18,2),
    TotalClaimPaymentAmount DECIMAL(18,2),
    PatientResponsibility DECIMAL(18,2),
    AdjustmentGroupCode NVARCHAR(10),
    AdjustmentReasonCode NVARCHAR(10),
    AdjustmentAmount DECIMAL(18,2),
    ServiceDateFrom DATETIME2,
    ServiceDateTo DATETIME2,
    CorrelationId NVARCHAR(128),
    ProcessedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    Version INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_PaymentReconciliation_ClaimReconciliation FOREIGN KEY (ClaimReconciliationId) REFERENCES clearinghouse.ClaimReconciliation(Id),
    INDEX IX_PaymentReconciliation_ClaimId NONCLUSTERED (ClaimId),
    INDEX IX_PaymentReconciliation_CheckNumber NONCLUSTERED (CheckNumber),
    INDEX IX_PaymentReconciliation_PayerId NONCLUSTERED (PayerId),
    INDEX IX_PaymentReconciliation_CreatedAt NONCLUSTERED (CreatedAt DESC)
);
GO

-- ============================================================================
-- TABLE: StediTransactions
-- Purpose: Stedi API transaction tracking
-- ============================================================================
CREATE TABLE clearinghouse.StediTransactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TransactionId NVARCHAR(200) NOT NULL,
    TransactionType INT NOT NULL,
    Direction INT NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    RequestPayload NVARCHAR(MAX),
    ResponsePayload NVARCHAR(MAX),
    ApiEndpoint NVARCHAR(500),
    HttpStatusCode INT,
    CorrelationId NVARCHAR(128) NOT NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(4000),
    SubmittedAt DATETIME2,
    CompletedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    Version INT NOT NULL DEFAULT 1,
    INDEX IX_StediTransactions_TransactionId NONCLUSTERED (TransactionId),
    INDEX IX_StediTransactions_CorrelationId NONCLUSTERED (CorrelationId),
    INDEX IX_StediTransactions_Status NONCLUSTERED (Status),
    INDEX IX_StediTransactions_CreatedAt NONCLUSTERED (CreatedAt DESC)
);
GO

-- ============================================================================
-- TABLE: Notifications
-- Purpose: Alert and notification records
-- ============================================================================
CREATE TABLE clearinghouse.Notifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Type NVARCHAR(100) NOT NULL,
    Severity NVARCHAR(50) NOT NULL,
    Title NVARCHAR(500) NOT NULL,
    Message NVARCHAR(4000) NOT NULL,
    CorrelationId NVARCHAR(128),
    IsRead BIT NOT NULL DEFAULT 0,
    IsAcknowledged BIT NOT NULL DEFAULT 0,
    AcknowledgedBy NVARCHAR(200),
    AcknowledgedAt DATETIME2,
    ExpiresAt DATETIME2,
    Properties NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    INDEX IX_Notifications_Type NONCLUSTERED (Type) INCLUDE (Severity),
    INDEX IX_Notifications_CreatedAt NONCLUSTERED (CreatedAt DESC),
    INDEX IX_Notifications_IsRead NONCLUSTERED (IsRead) INCLUDE (Type)
);
GO

-- ============================================================================
-- TABLE: AlertRules
-- Purpose: Configurable alert rule definitions
-- ============================================================================
CREATE TABLE clearinghouse.AlertRules (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000),
    EventType NVARCHAR(100) NOT NULL,
    Condition NVARCHAR(1000),
    Severity NVARCHAR(50) NOT NULL DEFAULT 'Warning',
    IsActive BIT NOT NULL DEFAULT 1,
    NotificationChannel NVARCHAR(100) NOT NULL DEFAULT 'Email',
    Recipients NVARCHAR(2000),
    CooldownMinutes INT NOT NULL DEFAULT 60,
    LastTriggeredAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    CreatedBy NVARCHAR(200),
    Version INT NOT NULL DEFAULT 1
);
GO

-- ============================================================================
-- TABLE: ClearinghouseConfiguration
-- Purpose: Clearinghouse connection and plugin configuration
-- ============================================================================
CREATE TABLE clearinghouse.ClearinghouseConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- SFTP, API, Hybrid
    IsActive BIT NOT NULL DEFAULT 1,
    Host NVARCHAR(500),
    Port INT,
    UploadDirectory NVARCHAR(500),
    DownloadDirectory NVARCHAR(500),
    ApiBaseUrl NVARCHAR(500),
    AuthType NVARCHAR(50), -- ApiKey, OAuth2, Certificate
    KeyVaultSecretPrefix NVARCHAR(200),
    MaxConcurrentConnections INT NOT NULL DEFAULT 5,
    TimeoutSeconds INT NOT NULL DEFAULT 180,
    RetryCount INT NOT NULL DEFAULT 3,
    RetryDelaySeconds INT NOT NULL DEFAULT 5,
    PluginAssembly NVARCHAR(500),
    PluginClassName NVARCHAR(500),
    ConfigJson NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    Version INT NOT NULL DEFAULT 1,
    INDEX IX_ClearinghouseConfiguration_Name UNIQUE NONCLUSTERED (Name)
);
GO

-- ============================================================================
-- SEED DATA: Default Clearinghouse Configurations
-- ============================================================================
INSERT INTO clearinghouse.ClearinghouseConfiguration (Name, Type, IsActive, AuthType, KeyVaultSecretPrefix, PluginAssembly, PluginClassName)
VALUES
    ('Ability', 'SFTP', 1, 'Password', 'ability', 'ClearinghousePlugins.Ability', 'ClearinghousePlugins.Ability.AbilityPlugin'),
    ('Availity', 'SFTP', 1, 'Password', 'availity', 'ClearinghousePlugins.Availity', 'ClearinghousePlugins.Availity.AvailityPlugin'),
    ('TriZetto', 'SFTP', 0, 'Password', 'trizetto', 'ClearinghousePlugins.TriZetto', 'ClearinghousePlugins.TriZetto.TriZettoPlugin'),
    ('Sandata', 'SFTP', 0, 'Password', 'sandata', 'ClearinghousePlugins.Sandata', 'ClearinghousePlugins.Sandata.SandataPlugin'),
    ('Waystar', 'SFTP', 0, 'Password', 'waystar', 'ClearinghousePlugins.Waystar', 'ClearinghousePlugins.Waystar.WaystarPlugin'),
    ('Stedi', 'Hybrid', 1, 'ApiKey', 'stedi', 'ClearinghousePlugins.Stedi', 'ClearinghousePlugins.Stedi.StediPlugin');
GO

-- ============================================================================
-- STORED PROCEDURES
-- ============================================================================

-- Get file processing statistics by clearinghouse
CREATE OR ALTER PROCEDURE clearinghouse.GetProcessingStatistics
    @ClearinghouseId INT = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        cf.ClearinghouseId,
        cf.ClearinghouseName,
        cf.TransactionType,
        COUNT(*) AS TotalFiles,
        SUM(CASE WHEN cf.Status = 6 THEN 1 ELSE 0 END) AS ProcessedFiles,
        SUM(CASE WHEN cf.Status = 7 THEN 1 ELSE 0 END) AS FailedFiles,
        SUM(CASE WHEN cf.Status IN (0,1,2,3,4,5) THEN 1 ELSE 0 END) AS PendingFiles,
        AVG(DATEDIFF(SECOND, cf.ProcessingStartedAt, cf.ProcessingCompletedAt)) AS AvgProcessingSeconds,
        SUM(cf.FileSizeBytes) AS TotalBytesProcessed
    FROM clearinghouse.ClearinghouseFiles cf
    WHERE (@ClearinghouseId IS NULL OR cf.ClearinghouseId = @ClearinghouseId)
        AND (@StartDate IS NULL OR cf.CreatedAt >= @StartDate)
        AND (@EndDate IS NULL OR cf.CreatedAt <= @EndDate)
    GROUP BY cf.ClearinghouseId, cf.ClearinghouseName, cf.TransactionType
    ORDER BY cf.ClearinghouseName, cf.TransactionType;
END
GO

-- Get file timeline/lifecycle
CREATE OR ALTER PROCEDURE clearinghouse.GetFileTimeline
    @FileId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        feh.Id,
        feh.FileId,
        feh.EventType,
        feh.PreviousStatus,
        feh.NewStatus,
        feh.CorrelationId,
        feh.TriggeredBy,
        feh.ServiceName,
        feh.ErrorMessage,
        feh.CreatedAt
    FROM clearinghouse.FileEventHistory feh
    WHERE feh.FileId = @FileId
    ORDER BY feh.CreatedAt ASC;
END
GO

-- Partition strategy note for production:
-- For tables with high insert throughput (FilesRawLog, FileEventHistory, ClearinghouseFiles),
-- consider partitioning by CreatedAt (monthly or quarterly) for:
-- 1. Efficient archival via partition switching
-- 2. Improved query performance with partition elimination
-- 3. Parallel query execution across partitions
