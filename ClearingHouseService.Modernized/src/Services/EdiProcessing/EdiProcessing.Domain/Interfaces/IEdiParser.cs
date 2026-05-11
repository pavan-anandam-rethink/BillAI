using EdiProcessing.Domain.Entities;

namespace EdiProcessing.Domain.Interfaces;

public interface IEdiParser
{
    Task<EdiDocument> ParseAsync(Stream ediContent, string correlationId, CancellationToken cancellationToken = default);
    bool CanParse(string content);
}
