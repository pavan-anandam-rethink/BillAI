using Rethink.Services.Common.Models.EligibilityRequest;
using System.Threading;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Interfaces
{
    public interface IStediEligibilityClient
    {
        Task<Eligibility271ParsedResponse> Submit270Async(string edi270Stream, CancellationToken ct);
    }
}
