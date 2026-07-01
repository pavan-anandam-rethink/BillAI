-- =============================================================================
-- ClaimLifecycleMonitoring - Seed data
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [monitoring].[StageSlaConfigurations])
BEGIN
    INSERT INTO [monitoring].[StageSlaConfigurations] ([Id], [Stage], [SlaHours], [Description], [CreatedUtc])
    VALUES
        (NEWID(),  10,   2, N'Appointment Created',      SYSUTCDATETIME()),
        (NEWID(),  20,   4, N'Eligibility Verification', SYSUTCDATETIME()),
        (NEWID(),  30,  24, N'Authorization',            SYSUTCDATETIME()),
        (NEWID(),  40,   4, N'Claim Created',            SYSUTCDATETIME()),
        (NEWID(),  50,   2, N'Claim Validation',         SYSUTCDATETIME()),
        (NEWID(),  60,   2, N'837 Generated',            SYSUTCDATETIME()),
        (NEWID(),  70,  24, N'837 Submitted',            SYSUTCDATETIME()),
        (NEWID(),  80,  72, N'999 Received',             SYSUTCDATETIME()),
        (NEWID(),  90, 720, N'277CA Received',           SYSUTCDATETIME()),
        (NEWID(), 100, 720, N'Payer Processing',         SYSUTCDATETIME()),
        (NEWID(), 110,  24, N'835 Received',             SYSUTCDATETIME()),
        (NEWID(), 120,  12, N'Payment Posting',          SYSUTCDATETIME()),
        (NEWID(), 130,  24, N'Patient Invoice',          SYSUTCDATETIME()),
        (NEWID(), 140,   1, N'Completed',                SYSUTCDATETIME());
END
GO
