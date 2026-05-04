using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Messaging
{
    [ExcludeFromCodeCoverage]
    public static class Queues
    {
        public static string RT_Billing_EraDownloadStart => "rt-billing-queue-download";
        public static string RT_Billing_EraDownloadProcess => "rt-billing-queue-era-file-process";
        public static string RT_Billing_ReportUploadStart => "rt-billing-queue-reportuploadstart";
        public static string RT_Billing_ReportGenerateStart => "rt-billing-queue-reportgeneratestart";
        public static string RT_Billing_ClaimCreationEnd => "rt-billing-queue-claimcreateend";
        public static string RT_Billing_EdiUploadEnd => "rt-billing-queue-ediuploadend-development";
        public static string RT_Billing_EdiUploadStart => "rt-billing-queue-ediuploadstart-development";
        public static string RT_Billing_ClearingHouse_ClaimSubmission => "rt-billing-queue-clearinghouse-claim-submission";
        public static string RT_Billing_ClearingHouse_SFTPFiles_Download => "rt-billing-queue-clearinghouse-sftpfiles-download";
        public static string RT_Billing_Queue_AppointmentUpdate => "rt-billing-queue-appointmentupdate";
        public static string RT_Billing_Queue_AppointmentBillingStatus => "rt-billing-queue-appointmentbillingstatus";
    }
}