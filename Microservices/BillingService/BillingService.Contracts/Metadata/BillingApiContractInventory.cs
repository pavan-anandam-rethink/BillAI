namespace BillingService.Contracts.Metadata;

public static class BillingApiContractInventory
{
    public const string DefaultControllerRoute = "[controller]/[action]";
    public const string PusherAuthRoute = "pusher/auth";
    public const string HealthRoute = "/api/health";
    public const string SwaggerDocumentName = "v1";

    public static readonly IReadOnlyCollection<string> CompatibilityCriticalControllers =
    [
        "Claim",
        "ClaimPosting",
        "PaymentPosting",
        "BulkPaymentPosting",
        "PatientInvoice",
        "Appointment",
        "AppointmentReports",
        "ClientChargeHistory",
        "ClearingHouse",
        "BillingSettings",
        "FunderSetting"
    ];
}
