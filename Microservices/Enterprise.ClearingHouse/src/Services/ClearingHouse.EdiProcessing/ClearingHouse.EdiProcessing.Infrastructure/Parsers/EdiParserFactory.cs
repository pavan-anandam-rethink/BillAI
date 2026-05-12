using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.SharedKernel;

namespace ClearingHouse.EdiProcessing.Infrastructure.Parsers;

/// <summary>
/// Factory that resolves the appropriate <see cref="IEdiParser"/> based on the EDI transaction type.
/// </summary>
public sealed class EdiParserFactory
{
    /// <summary>
    /// Creates an <see cref="IEdiParser"/> instance for the specified transaction type.
    /// </summary>
    /// <param name="transactionType">The EDI transaction type to resolve a parser for.</param>
    /// <returns>The appropriate parser implementation.</returns>
    /// <exception cref="NotSupportedException">Thrown when the transaction type is not yet supported.</exception>
    public IEdiParser CreateParser(EdiTransactionType transactionType)
    {
        return transactionType switch
        {
            EdiTransactionType.Claim837 => new Edi837Parser(),
            EdiTransactionType.Payment835 => new Edi835Parser(),
            EdiTransactionType.Acknowledgement999 => new Edi999Parser(),
            EdiTransactionType.ClaimStatus277 => new Edi277Parser(),
            _ => throw new NotSupportedException(
                $"EDI transaction type '{transactionType}' is not yet supported. " +
                "Supported types: 837 (Claims), 835 (Payments), 999 (Acknowledgements), 277 (Claim Status).")
        };
    }
}
