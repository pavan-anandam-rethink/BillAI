using AutoFixture;
using AutoFixture.Kernel;
using BillingService.XUnit.Tests.Common.Helpers.Fixture;
using System.Linq;

namespace BillingService.XUnit.Tests.Common
{
    public abstract class BaseTest
    {
        protected BaseTest()
        {
            CreateFixture();
        }

        // Changed from static to instance property to avoid race conditions in parallel tests
        protected IFixture Fixture { get; private set; }

        protected void CreateFixture()
        {
            // Initialize
            Fixture = new Fixture().ConfigureVirtualMembersBehavior();

            // Remove the default recursion behavior
            var behaviorsToRemove = Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList();
            foreach (var behavior in behaviorsToRemove)
            {
                Fixture.Behaviors.Remove(behavior);
            }

            // Add recursion-safe behavior globally
            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }
    }
}
