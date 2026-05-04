using System;

namespace Rethink.Services.Common.Enums.BH
{
    [Flags]
    public enum DayTypes
    {
        Mon = 1,
        Tue = 2,
        Wed = 4,
        Thu = 8,
        Fri = 16,
        Sat = 32,
        Sun = 64,
    }
}