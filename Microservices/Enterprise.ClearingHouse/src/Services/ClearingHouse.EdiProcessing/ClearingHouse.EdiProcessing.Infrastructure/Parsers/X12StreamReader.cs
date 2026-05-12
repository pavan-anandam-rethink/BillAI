using ClearingHouse.EdiProcessing.Domain.Parsers;

namespace ClearingHouse.EdiProcessing.Infrastructure.Parsers;

/// <summary>
/// Stream-based X12 segment reader that processes EDI files character by character
/// without loading the entire file into memory.
/// </summary>
public sealed class X12StreamReader : ISegmentReader, IAsyncDisposable
{
    private const char SegmentTerminator = '~';
    private const char ElementSeparator = '*';
    private const char SubElementSeparator = ':';

    private readonly StreamReader _reader;
    private int _sequenceNumber;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="X12StreamReader"/> class.
    /// </summary>
    /// <param name="stream">The input stream containing EDI data.</param>
    /// <param name="bufferSize">The buffer size for the underlying StreamReader.</param>
    public X12StreamReader(Stream stream, int bufferSize = 8192)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _reader = new StreamReader(stream, bufferSize: bufferSize, leaveOpen: false);
        _sequenceNumber = 0;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<EdiRawSegment> ReadSegmentsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new System.Text.StringBuilder(256);

        while (!_reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var charValue = await _reader.ReadAsync().ConfigureAwait(false);
            if (charValue == -1)
                break;

            var currentChar = (char)charValue;

            if (currentChar == SegmentTerminator)
            {
                var segmentData = buffer.ToString().Trim();
                buffer.Clear();

                if (string.IsNullOrWhiteSpace(segmentData))
                    continue;

                var segmentIdentifier = ExtractSegmentIdentifier(segmentData);
                _sequenceNumber++;

                yield return new EdiRawSegment(
                    SegmentIdentifier: segmentIdentifier,
                    RawData: segmentData,
                    SequenceNumber: _sequenceNumber,
                    ElementSeparator: ElementSeparator,
                    SubElementSeparator: SubElementSeparator);
            }
            else
            {
                buffer.Append(currentChar);
            }
        }

        // Handle any remaining data without a terminator
        if (buffer.Length > 0)
        {
            var remainingData = buffer.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(remainingData))
            {
                var segmentIdentifier = ExtractSegmentIdentifier(remainingData);
                _sequenceNumber++;

                yield return new EdiRawSegment(
                    SegmentIdentifier: segmentIdentifier,
                    RawData: remainingData,
                    SequenceNumber: _sequenceNumber,
                    ElementSeparator: ElementSeparator,
                    SubElementSeparator: SubElementSeparator);
            }
        }
    }

    private static string ExtractSegmentIdentifier(string segmentData)
    {
        var separatorIndex = segmentData.IndexOf(ElementSeparator);
        return separatorIndex > 0
            ? segmentData[..separatorIndex]
            : segmentData;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _reader.Dispose();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
    }
}
