/*
BillingService blob metadata table.

Stores lightweight operational metadata for blob artifacts:
  - BlobName     : logical blob name within the container (never a file path)
  - ContainerName: the Azure Blob Storage container
  - CorrelationId: links the blob to a billing transaction for tracing

This table is NOT the authoritative source of blob content.
Azure Blob Storage itself is the authoritative source.
File paths, blob URLs, and physical storage references are NEVER stored here.

Safe to run multiple times (idempotent).
*/

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'billing')
BEGIN
    EXEC('CREATE SCHEMA billing AUTHORIZATION dbo');
END;

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'billing.BillingBlobMetadata') AND type = 'U')
BEGIN
    CREATE TABLE billing.BillingBlobMetadata (
        Id              UNIQUEIDENTIFIER    NOT NULL CONSTRAINT PK_BillingBlobMetadata PRIMARY KEY DEFAULT NEWID(),
        BlobName        NVARCHAR(1024)      NOT NULL,
        ContainerName   NVARCHAR(256)       NOT NULL,
        CorrelationId   NVARCHAR(128)       NULL,
        CreatedOnUtc    DATETIMEOFFSET(7)   NOT NULL CONSTRAINT DF_BillingBlobMetadata_CreatedOnUtc DEFAULT SYSUTCDATETIME()
    );

    -- Unique constraint prevents duplicate blob registrations
    CREATE UNIQUE NONCLUSTERED INDEX UX_BillingBlobMetadata_BlobName_ContainerName
        ON billing.BillingBlobMetadata (BlobName, ContainerName);

    -- Index for correlation-based retrieval (primary access pattern)
    CREATE NONCLUSTERED INDEX IX_BillingBlobMetadata_CorrelationId
        ON billing.BillingBlobMetadata (CorrelationId)
        INCLUDE (BlobName, ContainerName)
        WHERE CorrelationId IS NOT NULL;
END;
