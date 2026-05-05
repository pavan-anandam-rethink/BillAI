namespace Rethink.Services.Common.Enums.Billing
{
    public enum ExternalCodeType : int
    {
        Unknown = 0,
        ClaimStatusCode = 508,
        ClaimStatusCategoryCode = 507,
        ClaimAdjustmentGroupCode = 974,
        ClaimAdjustmentReasonCodes = 139,
        RemittanceAdviceRemarkCode = 411,
        SegmentSyntaxErrorCode = 620,
        ElementSyntaxErrorCode = 621,
    }
}
