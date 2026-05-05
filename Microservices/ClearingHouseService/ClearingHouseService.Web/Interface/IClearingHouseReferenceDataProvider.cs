using Rethink.Services.Common.Dtos.ClearingHouse;

namespace ClearingHouseService.Web.Interface
{
    public interface IClearingHouseReferenceDataProvider
    {
        Task<string> GetPayersAsync(CancellationToken ct);
        Task<string> GetEnrollmentsAsync(CancellationToken ct);
    }
}
