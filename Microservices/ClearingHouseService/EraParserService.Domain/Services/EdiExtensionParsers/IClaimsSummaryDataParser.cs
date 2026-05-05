using System.Threading.Tasks;

namespace EraParserService.Domain.Services.EdiExtensionParsers
{
    public interface IClaimsSummaryDataParser
    {
        Task ParseAsync(int accountInfoId, string fileId, string data);
    }
}
