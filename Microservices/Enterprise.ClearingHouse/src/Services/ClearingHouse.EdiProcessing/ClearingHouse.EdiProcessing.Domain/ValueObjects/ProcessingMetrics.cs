namespace ClearingHouse.EdiProcessing.Domain.ValueObjects;

/// <summary>
/// Represents metrics collected during EDI file processing.
/// </summary>
/// <param name="TotalSegments">The total number of segments in the EDI file.</param>
/// <param name="ProcessedSegments">The number of segments successfully processed.</param>
/// <param name="FailedSegments">The number of segments that failed processing.</param>
/// <param name="ProcessingDurationMs">The total processing duration in milliseconds.</param>
/// <param name="ThroughputSegmentsPerSecond">The processing throughput in segments per second.</param>
public sealed record ProcessingMetrics(
    int TotalSegments,
    int ProcessedSegments,
    int FailedSegments,
    long ProcessingDurationMs,
    double ThroughputSegmentsPerSecond)
{
    /// <summary>
    /// Gets the success rate as a percentage (0.0 to 100.0).
    /// Returns 0.0 if there are no total segments.
    /// </summary>
    public double SuccessRate => TotalSegments > 0
        ? (double)ProcessedSegments / TotalSegments * 100.0
        : 0.0;
}
