using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.PatientInvoice
{
    public interface IClientInfoService
    {
        Task<ClientInfo> GetClientInfo(int accountId, int clientId);
        Task<BillingProviderInfo> GetBillingProviderInfo(int accountId, int clientId);
    }
}
