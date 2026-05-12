using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Domain.Interfaces;

/// <summary>
/// Defines a parser for reading and parsing EDI file streams into segments.
/// </summary>
public interface IEdiParser
{
    /// <summary>
    /// Parses an EDI stream and yields parsed segments asynchronously.
    /// </summary>
    /// <param name="stream">The EDI file stream to parse.</param>
    /// <param name="transactionType">The EDI transaction type.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of parsed <see cref="EdiSegment"/> instances.</returns>
    IAsyncEnumerable<EdiSegment> ParseStreamAsync(
        Stream stream,
        EdiTransactionType transactionType,
        CancellationToken cancellationToken);
}
