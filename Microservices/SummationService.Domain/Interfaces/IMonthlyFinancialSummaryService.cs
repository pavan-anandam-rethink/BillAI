using Rethink.Services.Common.Models.ReportingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummationService.Domain.Interfaces
{
    public interface IMonthlyFinancialSummaryService
    {
        Task<MonthlyFinancialSummaryResponse> GetMonthlyFinancialSummaryAsync(int AccountInfoId, DateTime startDate, DateTime endDate, string dateType = "Transaction", IEnumerable<int>? locationIds = null, IEnumerable<int>? funderIds = null);

        Task<byte[]> ExportToExcelAsync(MonthlyFinancialSummaryRequest model, MonthlyFinancialSummaryResponse response, CancellationToken cancellationToken);
    }
}
