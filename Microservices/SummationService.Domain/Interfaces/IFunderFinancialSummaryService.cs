using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rethink.Services.Common.Models.ReportingModels;

namespace SummationService.Domain.Interfaces
{
    public interface IFunderFinancialSummaryService
    {
        Task<FunderFinancialSummaryResponse> GetFunderFinancialSummaryAsync(
            int accountInfoId,
            DateTime startDate, 
            DateTime endDate, 
            string dateType = "Transaction", 
            IEnumerable<int>? locationIds = null, 
            IEnumerable<int>? funderIds = null,
            IEnumerable<int>? renderingProviderIds = null,
            IEnumerable<int>? billingProviderIds = null);

        Task<byte[]> ExportToExcelAsync(FunderFinancialSummaryRequest model, FunderFinancialSummaryResponse response, CancellationToken cancellationToken);

    }
}
