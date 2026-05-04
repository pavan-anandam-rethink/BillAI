using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Messaging;
/// <summary>
/// Azure Service Bus Messaging Topics
/// </summary>
[ExcludeFromCodeCoverage]
public static class Topics
{
    public static string RT_Billing_EraDownload => "rt-billing-eradownload";
    public static string RT_Billing_EraDownloadStart => "rt-billing-eradownloadstart";
    public static string RT_Billing_ProcessClaimTxn => "rt-billing-processclaimtxn";
    public static string RT_Billing_ProcessClaimSubmission => "claim-submission-topic";
    public static string RT_Billing_ClaimApproval => "claim-approval-topic";
}