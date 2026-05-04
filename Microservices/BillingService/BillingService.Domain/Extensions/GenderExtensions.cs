using BillingService.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class GenderExtensions
    {
        public static string ToCode(this Gender gender)
        {
            return gender switch
            {
                Gender.Male => "M",
                Gender.Female => "F",
                _ => "U"
            };
        }
    }
}
