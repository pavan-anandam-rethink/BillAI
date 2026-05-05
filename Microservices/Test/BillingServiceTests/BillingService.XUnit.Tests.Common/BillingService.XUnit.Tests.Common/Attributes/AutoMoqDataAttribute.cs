using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using BillingService.XUnit.Tests.Common.Helpers.Fixture;

namespace BillingService.XUnit.Tests.Common.Attributes
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() : base(InitializeFixture) { }

        private static IFixture InitializeFixture()
        {
            var fixture = new Fixture()
                .ConfigureVirtualMembersBehavior()
                .Customize(new AutoMoqCustomization());

            return fixture;
        }
    }
}
