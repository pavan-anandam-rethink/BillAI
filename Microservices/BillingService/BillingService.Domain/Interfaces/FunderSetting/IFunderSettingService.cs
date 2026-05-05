using BillingService.Domain.Models.BillingSettings;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.FunderSetting;

public interface IFunderSettingService
{
    Task UpdateFunderSettingsAsync(FunderSettingRequest model);
}