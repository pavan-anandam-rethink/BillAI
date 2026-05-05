namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimTransactionType
    {
        billedAmount = 1,
        insurancePayment = 2,
        patientPayment = 3,
        adjustment = 4,
        patientResponsibility = 5,
        writeOff = 6,
        otherPayment = 7,
        deleteCharge = 8,
        deleteChargePayment = 9,//Disassociate payments and adjustments (including PR) from the charge
        deleteClaim = 10,
        submitClaim = 11,
        newDay = 12,//For AR only
        updatePaymentSummary = 13,//TODO
        eraReceived = 14, //For EraPaymentsOnly
    }
}
