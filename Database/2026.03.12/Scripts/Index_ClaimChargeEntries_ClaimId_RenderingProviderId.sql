/*============================================================== 
Index Creation Script
Purpose : Optimize Rendering Provider search in claims screen Table : dbo.ClaimChargeEntries
Author : Application Team

==============================================================*/
BEGIN TRY 

BEGIN TRANSACTION 

IF NOT EXISTS ( SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimChargeEntries_Claim_RenderingProvider' AND object_id = OBJECT_ID('dbo.ClaimChargeEntries') ) 

BEGIN PRINT 'Creating index IX_ClaimChargeEntries_Claim_RenderingProvider'; 

CREATE NONCLUSTERED INDEX IX_ClaimChargeEntries_Claim_RenderingProvider 

ON dbo.ClaimChargeEntries (hcClaimId, RenderingProviderId) 

WHERE DateDeleted IS NULL;


PRINT 'Index created successfully'; 

END	

ELSE 
BEGIN PRINT 'Index already exists';

END 

COMMIT TRANSACTION

END 

TRY BEGIN 
CATCH 
PRINT 'Error occurred. Rolling back...'
IF @@TRANCOUNT > 0 

ROLLBACK TRANSACTION
THROW;
END CATCH
GO