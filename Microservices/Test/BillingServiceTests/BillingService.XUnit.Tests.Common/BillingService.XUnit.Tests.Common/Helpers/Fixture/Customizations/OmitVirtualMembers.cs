using AutoFixture.Kernel;
using System.Reflection;

namespace BillingService.XUnit.Tests.Common
{
    internal class OmitVirtualMembers : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            PropertyInfo propertyInfo = request as PropertyInfo;
            if (propertyInfo != null && propertyInfo.GetGetMethod().IsVirtual && !propertyInfo.PropertyType.IsPrimitive)
            {
                return new OmitSpecimen();
            }

            return new NoSpecimen();
        }
    }
}
