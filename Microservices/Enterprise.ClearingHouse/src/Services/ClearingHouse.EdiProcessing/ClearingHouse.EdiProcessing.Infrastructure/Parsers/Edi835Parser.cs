using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Infrastructure.Parsers;

/// <summary>
/// EDI 835 (Electronic Remittance Advice / Payment) parser implementation.
/// Processes ERA files and extracts payment-related segments.
/// </summary>
public sealed class Edi835Parser : IEdiParser
{
    private static readonly HashSet<string> PaymentRelevantSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "CLP", "SVC", "CAS", "AMT", "BPR", "TRN", "PLB", "REF",
        "DTM", "NM1", "N1", "N3", "N4", "PER", "MIA", "MOA",
        "QTY", "LX", "TS3", "TS2", "RDM",
        "ISA", "GS", "ST", "SE", "GE", "IEA"
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
            if (!PaymentRelevantSegments.Contains(rawSegment.SegmentIdentifier))
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
