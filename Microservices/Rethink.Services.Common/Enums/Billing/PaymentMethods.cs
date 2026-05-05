using System.ComponentModel;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum PaymentMethods
    {
        [Description("Cash")]
        Cash = 1,
        [Description("Check")]
        Check = 2,
        [Description("ACH")]
        ACH = 3,
        [Description("Transfer")]
        Transfer = 4,
        [Description("Credit Card")]
        CreditCard = 5,
        [Description("Non-Payment")]
        NonPayment = 6,
        [Description("FSA/HSA")]
        FSAHSA = 7,
        [Description("RevSpring")]
        RevSpring = 9
        // commenting out for bug 229113
        //[Description("ERA")]
        //ERA = 8
    }
}
