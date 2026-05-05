using System.Runtime.Serialization;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum WriteOffReasonDescription
    {
        [EnumMember(Value = "Financial Hardship")]
        financialHardship = 1,
        [EnumMember(Value = "MUE Adjustment")]
        mueAdjustment = 2,
        [EnumMember(Value = "Exceeding Authorization")]
        exceedingAuthorization = 3,
        [EnumMember(Value = "Bad Debt Adjustment")]
        badDebtAdjustment = 4,
        [EnumMember(Value = "Small Balance Adjustment")]
        smallBalanceAdjustment = 5,
        [EnumMember(Value = "Out of Network Adjustment")]
        outOfNetworkAdjustment = 6,
        [EnumMember(Value = "Prompt Pay Discount")]
        promptPayDiscount = 7,
        [EnumMember(Value = "Other")]
        other = 8
    }
}
