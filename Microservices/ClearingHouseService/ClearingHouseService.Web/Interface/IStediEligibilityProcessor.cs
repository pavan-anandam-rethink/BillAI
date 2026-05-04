using Rethink.Services.Common.Dtos.ClearingHouse;

namespace ClearingHouseService.Web.Interface
{
    public interface IStediEligibilityProcessor
    {
        Task ProcessAsync(StediEligibilityJobDTO job, CancellationToken cancellationToken);
    }
}
