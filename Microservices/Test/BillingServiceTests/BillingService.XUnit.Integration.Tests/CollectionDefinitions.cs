using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    public class CollectionDefinitions
    {
        [CollectionDefinition("Billing")]
        public class BillingFixtureDefinition : ICollectionFixture<TestServerFixture> { }
    }
}
