namespace ClearingHouse.EdiProcessing.Domain.Parsers;

/// <summary>
/// Defines a low-level reader for reading raw EDI segments from a stream.
/// </summary>
public interface ISegmentReader : IAsyncDisposable
{
    /// <summary>
    /// Reads the next raw EDI segment from the stream.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The next <see cref="EdiRawSegment"/>, or <c>null</c> if no more segments are available.</returns>
    Task<EdiRawSegment?> ReadNextSegmentAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Represents a raw EDI segment read from a stream before parsing.
/// </summary>
/// <param name="Content">The raw content of the segment.</param>
/// <param name="SegmentIdentifier">The segment identifier string (e.g., "ISA", "CLM").</param>
/// <param name="SequenceNumber">The sequence number of the segment in the file.</param>
public sealed record EdiRawSegment(
    string Content,
    string SegmentIdentifier,
    int SequenceNumber);
