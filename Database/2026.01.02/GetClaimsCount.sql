/****** Object:  StoredProcedure [dbo].[GetClaimsCount]    Script Date: 29-12-2025 13:52:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [dbo].[GetClaimsCount] -- EXEC [GetClaimsCount] 18421, NULL, 0  
(  
 @AccountInfoId INT,  
 @ClaimNumber NVARCHAR(20) = null,  
 @ShowVoided BIT  
)  
AS  
BEGIN  
    SET NOCOUNT ON  
    
 DECLARE @DeniedCount INT = 0;  
  
    SELECT DISTINCT     
            [c].[Id] AS Id,  
   [c].claimIdentifier AS ClaimNumber  
 INTO #temp  
   
    FROM [Claims] AS [c]  
    LEFT JOIN [ClaimChargeEntries] AS [cce] ON [c].[id] = [cce].[hcClaimId]  
 WHERE ([c].[AccountInfoId] = @AccountInfoId) AND [c].[DateDeleted] IS NULL  
 AND [c].[ClaimIdentifier] LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END  
  
 set @DeniedCount = (  
  select count(*) from Claims c  
  JOIN PaymentClaim pc on pc.hcClaimid = c.id   
  where c.hcClaimStatusId=5 and c.dateDeleted is null and c.isFlagged = 0 and pc.claimStatus=4  
  and c.accountInfoId = @AccountInfoId  
 )  
  
 print @DeniedCount  
  
 SELECT   
  COUNT (CASE WHEN (claim.[hcClaimStatusId] = 1 OR claim.[hcClaimStatusId] = 20) AND claim.[isFlagged] = 0 THEN 1 END) AS PendingReviewTotalCount,  
  COUNT (CASE WHEN (claim.[hcClaimStatusId] = 2 OR claim.[hcClaimStatusId] = 7 OR claim.[hcClaimStatusId] = 10 OR claim.[hcClaimStatusId] = 14 OR claim.[hcClaimStatusId] = 19) AND claim.[isFlagged] = 0 THEN 1 END) AS ReadyToBillTotalCount,  
  COUNT (CASE WHEN (claim.[hcClaimStatusId] = 3 OR claim.[hcClaimStatusId] = 4 OR claim.[hcClaimStatusId] = 11 OR claim.[hcClaimStatusId] = 12 OR claim.[hcClaimStatusId] = 15 OR claim.[hcClaimStatusId] = 16 OR claim.[hcClaimStatusId] = 17) AND claim.[isFlagged] = 0 THEN 1 END) AS BillingPendingTotalCount,  
  COUNT (CASE WHEN ((@ShowVoided = 1 AND claim.[hcClaimStatusId] = 18) OR claim.[hcClaimStatusId] = 6 OR claim.[hcClaimStatusId] = 13) AND claim.[isFlagged] = 0 THEN 1 END) AS ClosedTotalCount,  
  COUNT (CASE WHEN (claim.[hcClaimStatusId] = 8 OR claim.[hcClaimStatusId] = 9) AND claim.[isFlagged] = 0 THEN 1 END) AS RejectedTotalCount,  
  --@DeniedCount as DeniedTotalCount,  
  COUNT (CASE WHEN (claim.[hcClaimStatusId] = 5) AND claim.[isFlagged] = 0 THEN 1 END) AS DeniedTotalCount,  
  COUNT (CASE WHEN claim.[isFlagged] = 1 THEN 1 END) AS FlaggedTotalCount   
 FROM #temp t  
 LEFT JOIN Claims AS claim ON claim.id = t.Id  
  
 DROP TABLE #temp  
END