using Rethink.Services.Common.Handlers;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimCreateService
    {
        Task ProcessClaimCreation(ClaimCreateEnd model);
    }
}
