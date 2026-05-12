using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Infrastructure.Parsers;

/// <summary>
/// EDI 999 (Implementation Acknowledgement) parser implementation.
/// Processes acknowledgement files to determine acceptance or rejection of submitted transactions.
/// </summary>
public sealed class Edi999Parser : IEdiParser
{
    private static readonly HashSet<string> AcknowledgementSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "IK3", "IK4", "IK5", "AK1", "AK2", "AK9", "CTX",
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
            if (!AcknowledgementSegments.Contains(rawSegment.SegmentIdentifier))
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
