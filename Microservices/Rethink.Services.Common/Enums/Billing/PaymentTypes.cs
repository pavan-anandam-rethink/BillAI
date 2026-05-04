using System.ComponentModel;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum PaymentTypes
    {
        [Description("Insurance")]
        InsurancePayment = 1,
        [Description("Received ERA")]
        ERAReceived = 2,
        [Description("Patient")]
        ClientPayment = 3,
        [Description("Other")]
        OtherPayment = 4,
        Adjustment = 5
    }
}
