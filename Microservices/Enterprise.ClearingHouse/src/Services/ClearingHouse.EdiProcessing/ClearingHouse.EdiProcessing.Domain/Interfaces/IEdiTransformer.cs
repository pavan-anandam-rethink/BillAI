using ClearingHouse.EdiProcessing.Domain.Entities;

namespace ClearingHouse.EdiProcessing.Domain.Interfaces;

/// <summary>
/// Defines a transformer for converting parsed EDI segments into domain objects.
/// </summary>
public interface IEdiTransformer
{
    /// <summary>
    /// Transforms an EDI segment into a structured result asynchronously.
    /// </summary>
    /// <param name="segment">The EDI segment to transform.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="TransformedSegment"/> containing the transformation result.</returns>
    Task<TransformedSegment> TransformAsync(
        EdiSegment segment,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents the result of transforming an EDI segment.
/// </summary>
/// <param name="SegmentId">The identifier of the source segment.</param>
/// <param name="TransformedContent">The transformed content as a string.</param>
/// <param name="ClaimData">The extracted claim transaction data, if applicable.</param>
public sealed record TransformedSegment(
    Guid SegmentId,
    string TransformedContent,
    ClaimTransaction? ClaimData);
