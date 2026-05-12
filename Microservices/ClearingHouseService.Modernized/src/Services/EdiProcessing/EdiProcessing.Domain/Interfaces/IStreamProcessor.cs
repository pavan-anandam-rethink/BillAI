namespace EdiProcessing.Domain.Interfaces;

/// <summary>
/// Processes large EDI files using streaming to avoid memory overconsumption.
/// Supports chunked processing for files larger than 100GB.
/// </summary>
public interface IStreamProcessor
{
    /// <summary>
    /// Process a stream in chunks, invoking the processor for each chunk.
    /// </summary>
    Task ProcessStreamAsync(
        Stream inputStream,
        Func<ReadOnlyMemory<byte>, int, CancellationToken, Task> chunkProcessor,
        int chunkSizeBytes = 4 * 1024 * 1024,
        CancellationToken cancellationToken = default);
}
