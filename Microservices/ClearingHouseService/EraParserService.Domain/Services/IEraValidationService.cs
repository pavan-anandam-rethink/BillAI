using Rethink.Services.Common.Entities.Billing.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services
{
    public interface IEraValidationService
    {
        Task ValidateEraPayments(int accountInfoId, string fileID, List<PaymentEntity> eraPayments);
    }
}