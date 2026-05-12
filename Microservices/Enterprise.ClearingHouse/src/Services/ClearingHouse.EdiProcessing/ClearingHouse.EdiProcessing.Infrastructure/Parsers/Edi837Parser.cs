using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Infrastructure.Parsers;

/// <summary>
/// EDI 837 (Professional/Institutional Claim) parser implementation.
/// Processes claim submission files and extracts claim-relevant segments.
/// </summary>
public sealed class Edi837Parser : IEdiParser
{
    private static readonly HashSet<string> ClaimRelevantSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "CLM", "SV1", "SV2", "HI", "NM1", "SBR", "DTP", "REF",
        "AMT", "CN1", "DMG", "PAT", "PER", "N3", "N4", "PRV",
        "LX", "CRC", "HCP", "ISA", "GS", "ST", "BHT", "HL",
        "SE", "GE", "IEA"
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
            if (!ClaimRelevantSegments.Contains(rawSegment.SegmentIdentifier))
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
