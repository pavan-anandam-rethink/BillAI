using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Domain.Interfaces;

/// <summary>
/// Defines the EDI processing pipeline that orchestrates parsing, validation, and transformation.
/// </summary>
public interface IEdiProcessingPipeline
{
    /// <summary>
    /// Processes an EDI file stream through the full pipeline.
    /// </summary>
    /// <param name="stream">The EDI file stream to process.</param>
    /// <param name="ediFile">The EDI file aggregate root.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="ProcessingMetrics"/> collected during processing.</returns>
    Task<ProcessingMetrics> ProcessFileAsync(
        Stream stream,
        EdiFile ediFile,
        CancellationToken cancellationToken);
}
