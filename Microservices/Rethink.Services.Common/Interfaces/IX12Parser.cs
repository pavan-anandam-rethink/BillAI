using Rethink.Services.Common.Models.EligibilityRequest;

namespace Rethink.Services.Common.Interfaces
{
    public interface IX12Parser<out T>
    { 
        /// <summary>
      /// Parses a raw X12 payload and returns a domain-specific result
      /// </summary>
        T Parse(string x12);
    }
}
