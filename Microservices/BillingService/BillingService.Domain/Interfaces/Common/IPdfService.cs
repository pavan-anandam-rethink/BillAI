using BillingService.Domain.DataObjects.CompanyAccount;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Common
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePDF(string htmlContent);
    }
}
