using EdiFabric.Core.Model.Edi;
using Rethink.Services.Common.Handlers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services.EdiParsers.Edi999
{
    public interface IEdi999Parser
    {
        Task ParseAsync(EdiDownloadData ediDownloadData, List<IEdiItem> ediItems, int? transactionNumber, string fileName, byte[] data);
    }
}
