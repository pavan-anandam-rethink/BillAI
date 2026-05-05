using Rethink.Services.Common.Models.ReportingModels;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Interfaces;

public interface IReportService
{
    Task<bool> SendMonthlyReportAsync(ReportQueryModel dateRange);
    Task<bool> SendWeeklyReportAsync(ReportQueryModel dateRange);
}
