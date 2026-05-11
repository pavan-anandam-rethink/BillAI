using EdiProcessing.Domain.Entities;
using EdiProcessing.Domain.Interfaces;
using ClearingHouse.SharedKernel.Enums;
using EdiProcessing.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EdiProcessing.Infrastructure.Parsers;

public class BasicEdiParser : IEdiParser
{
    private readonly ILogger<BasicEdiParser> _logger;

    public BasicEdiParser(ILogger<BasicEdiParser> logger) => _logger = logger;

    public async Task<EdiDocument> ParseAsync(Stream ediContent, string correlationId, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(ediContent);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var transactionSet = new EdiTransactionSet(EdiTransactionType.Claim837, "000000001", "SENDER", "RECEIVER");
        var document = EdiDocument.Create(Guid.NewGuid(), EdiTransactionType.Claim837, transactionSet, correlationId);

        var segments = content.Split('~', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            var segmentContent = segments[i].Trim();
            if (string.IsNullOrEmpty(segmentContent)) continue;

            var segmentId = segmentContent.Split('*').FirstOrDefault() ?? "UNK";
            var segment = EdiSegment.Create(document.Id, segmentId, i + 1, segmentContent);
            document.AddSegment(segment);
        }

        document.SetTransactionCount(1);
        _logger.LogInformation("Parsed EDI document with {SegmentCount} segments", document.TotalSegments);
        return document;
    }

    public bool CanParse(string content) => content.Contains("ISA") || content.Contains("ST");
}
