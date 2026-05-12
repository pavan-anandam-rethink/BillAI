using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Infrastructure.Parsers;

/// <summary>
/// EDI 277 (Claim Status Response) parser implementation.
/// Processes claim status notification files to track claim adjudication status.
/// </summary>
public sealed class Edi277Parser : IEdiParser
{
    private static readonly HashSet<string> ClaimStatusSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "STC", "TRN", "REF", "DTP", "NM1", "HL", "AMT", "QTY",
        "ISA", "GS", "ST", "BHT", "SE", "GE", "IEA"
    };

    /// <inheritdoc />
    public async IAsyncEnumerable<EdiSegment> ParseStreamAsync(
        Stream stream,
        Guid fileId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var reader = new X12StreamReader(stream);

        await foreach (var rawSegment in reader.ReadSegmentsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!ClaimStatusSegments.Contains(rawSegment.SegmentIdentifier))
                continue;

            var segmentIdentifier = EdiSegmentIdentifier.Create(rawSegment.SegmentIdentifier);

            var segment = EdiSegment.Create(
                fileId: fileId,
                segmentIdentifier: segmentIdentifier,
                rawData: rawSegment.RawData,
                sequenceNumber: rawSegment.SequenceNumber);

            yield return segment;
        }
    }
}
