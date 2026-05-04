using System.ComponentModel;

namespace Rethink.Services.Common.Enums.Billing;

public enum MonthlyFrequency
{
    [Description("First Day")]
    FirstDay = 1,

    [Description("Last Day")]
    LastDay = 2
}