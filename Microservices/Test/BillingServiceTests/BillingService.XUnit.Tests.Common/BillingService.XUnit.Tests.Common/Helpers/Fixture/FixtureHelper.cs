using AutoFixture;
using System.Linq;

namespace BillingService.XUnit.Tests.Common.Helpers.Fixture
{
    public static class FixtureHelper
    {
        public static IFixture ConfigureVirtualMembersBehavior(this IFixture fixture)
        {
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(delegate (ThrowingRecursionBehavior b)
            {
                fixture.Behaviors.Remove(b);
            });
            fixture.Customizations.OfType<OmitVirtualMembers>().ToList().ForEach(delegate (OmitVirtualMembers b)
            {
                fixture.Customizations.Remove(b);
            });
            fixture.Customizations.Add(new OmitVirtualMembers());
            return fixture;
        }
    }
}
