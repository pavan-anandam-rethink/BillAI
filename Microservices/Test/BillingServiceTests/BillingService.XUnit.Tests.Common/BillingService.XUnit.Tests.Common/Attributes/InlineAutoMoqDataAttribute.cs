using AutoFixture.Xunit2;
using Xunit;

namespace BillingService.XUnit.Tests.Common.Attributes
{
    public class InlineAutoMoqDataAttribute : CompositeDataAttribute
    {
        public InlineAutoMoqDataAttribute(params object[] values) : this(new AutoMoqDataAttribute(), values) { }

        protected InlineAutoMoqDataAttribute(AutoMoqDataAttribute autoDataAttribute, params object[] values)
            : base(new InlineDataAttribute(values), autoDataAttribute)
        {

        }
    }
}
