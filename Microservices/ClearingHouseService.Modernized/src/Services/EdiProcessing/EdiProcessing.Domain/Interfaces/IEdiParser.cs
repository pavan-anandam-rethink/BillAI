using EdiProcessing.Domain.Entities;

namespace EdiProcessing.Domain.Interfaces;

public interface IEdiParser
{
    EdiFileType SupportedType { get; }
    Task<EdiParseResult> ParseAsync(Stream ediStream, EdiParseContext context, CancellationToken cancellationToken = default);
}

public record EdiParseContext(
    Guid FileId,
    string FileName,
    string ClearinghouseId,
    string CorrelationId,
    string? BatchId = null);

public record EdiParseResult(
    bool IsSuccess,
    int TotalSegments,
    int ProcessedSegments,
    int ErrorCount,
    IReadOnlyList<string> Errors,
    IDictionary<string, object>? Metadata = null);
