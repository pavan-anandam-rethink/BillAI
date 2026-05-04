using Rethink.Services.Common.Models.Claim;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Interfaces
{
    public interface IClearingHouseService
    {
        Task SubmitClaimAsync(ClaimSubmitModel submitModel);
    }
}
