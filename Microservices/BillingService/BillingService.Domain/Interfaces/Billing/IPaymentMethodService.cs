using BillingService.Domain.Models.PaymentPosting;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IPaymentMethodService
    {
        Task<PaymentMethodModel> GetPaymentMethodByName(string methodName);
        Task<PaymentMethodModel> GetPaymentMethodById(int methodId);
    }
}