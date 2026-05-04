
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Payment
{
    public interface IStediProviderEnrollmentService
    {
        Task<bool> VerifyProviderEnrollmentAsync(string providerNpi);
    }
}