using EdiFabric.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace EdiFabric.Templates.Common
{
    [ExcludeFromCodeCoverage]
    public static class EdiHelper
    {
        public static Assembly LoadFactory(MessageContext context)
        {
            if (context.Format.Equals("X12", StringComparison.Ordinal))
            {
                if (context.Version.StartsWith("005010X2", StringComparison.Ordinal))
                    return Assembly.Load("EdiFabric.Templates");
            }

            throw new Exception(string.Format("Transaction is not supported: Format {0} Version {1} Transaction ID {2} .", context.Format, context.Version, context.Name));
        }
    }
}
