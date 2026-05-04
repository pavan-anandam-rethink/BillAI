using System.Linq;

namespace BillingService.Domain.Extensions
{
    public static class FullNameExt
    {
        public static string GetFullName(string n1 = null, string n2 = null, string n3 = null)
        {
            return string.Join(' ', new[] { n1, n2, n3 }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}
