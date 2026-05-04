SET NOEXEC OFF
SET NOCOUNT ON
SET NUMERIC_ROUNDABORT OFF
GO

SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON
GO

SET XACT_ABORT ON
GO

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE

BEGIN TRANSACTION

IF @@ERROR <> 0 SET NOEXEC ON
GO

UPDATE dbo.claims  
SET LastBilledFunderId = PrimaryFunderId;

IF @@ERROR <> 0 SET NOEXEC ON
GO

--Rollback for ar status id
update [Reporting].[AccountsReceivable]
SET claimstatusid = 4
WHERE id = 130

IF @@ERROR <> 0 SET NOEXEC ON
GO

--Rollback for paymentsAdjustments
--update payadj
--SET payadj.eftOrCheckNumber = CASE
--	WHEN payadj.paymentId = 4017 THEN '100002425'
--	WHEN payadj.paymentId = 4020 and payadj.claimId=1107 THEN 'Insurance Funder'
--	WHEN payadj.paymentId = 4020 and payadj.claimId=1093 THEN 'testing'
--	WHEN payadj.paymentId = 4021 and payadj.claimId=1108 THEN 'rk'
--	WHEN payadj.paymentId = 4021 and payadj.claimId=1099 THEN 'rk'
--	WHEN payadj.paymentId = 4044 and payadj.claimId=1118 THEN 'rk1'
--	WHEN payadj.paymentId = 4051 and payadj.claimId=1121 THEN 't1'
--	WHEN payadj.paymentId = 4052 and payadj.claimId=1121 THEN 't1'
--	WHEN payadj.paymentId = 4052 and payadj.claimId=1121 THEN 't1'
--	WHEN payadj.paymentId = 4052 and payadj.claimId=1131 THEN 'bbbb'
--	WHEN payadj.paymentId = 4052 and payadj.claimId=1109 THEN 'Insurance-BCBS'
--	WHEN payadj.paymentId = 4058 and payadj.claimId=1139 THEN 'xztf'
--	WHEN payadj.paymentId = 4059 and payadj.claimId=1140 THEN 'xztf'
--	WHEN payadj.paymentId = 4061 and payadj.claimId=1143 THEN 'abc-insurance'
--	WHEN payadj.paymentId = 4061 and payadj.claimId=1143 THEN 'abc-insurance'
--	WHEN payadj.paymentId = 4067 and payadj.claimId=1119 THEN 't1'
--	WHEN payadj.paymentId = 4070 and payadj.claimId=1162 THEN 'bbbb'
--	WHEN payadj.paymentId = 4073 and payadj.claimId=1167 THEN '100002435'
--	WHEN payadj.paymentId = 4074 and payadj.claimId=1167 THEN '100002435'

--	ELSE payadj.eftOrCheckNumber
--END
--FROM
--[Reporting].[PaymentsAdjustments] payadj
--WHERE payadj.transactionType!=6 and payadj.paymentId IN (4017,4020,4021,4044,4051,4052,4058,4059,4061,
--4067,4070,4073,4074)


IF @@ERROR <> 0 SET NOEXEC ON
GO

--rollback for claimstatus table of reporting

UPDATE cs
SET cs.claimStatus = CASE
	WHEN cs.claimStatusId = 8 THEN 'Rejected ClearingHouse'
	WHEN cs.claimStatusId = 9 THEN 'Rejected Funder'
	WHEN cs.claimStatusId = 15 THEN 'Accepted ClearingHouse'
	WHEN cs.claimStatusId = 16 THEN 'Accepted Funder'
	WHEN cs.claimStatusId = 17 THEN 'Received Funder'
	WHEN cs.claimStatusId = 18 THEN 'Void Closed'
	ELSE cs.claimStatus
END
FROM 
[Reporting].[ClaimStatus] cs
WHERE cs.claimStatusId IN (8,9,15,16,17,18)

-- rollback for claim history action for scrubbing errors found.
UPDATE ClaimHistoryActions
SET name = 'ScrubbingErrorsFound', description = '@newValue Scrubbing Errors found' 
WHERE id = 16

IF @@ERROR <> 0 SET NOEXEC ON
GO


COMMIT TRANSACTION
GO

DECLARE @Success AS BIT
SET @Success=1
SET NOEXEC OFF
IF (@Success = 1) PRINT 'Database update succeeded'
ELSE BEGIN
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
	PRINT 'The database update failed. ROLLBACK TRANSACTION'
END
GO