using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;
using EdiProcessing.Domain.ValueObjects;

namespace EdiProcessing.Domain.Entities;

public class EdiDocument : AggregateRoot
{
    public Guid FileId { get; private set; }
    public EdiTransactionSet TransactionSet { get; private set; } = null!;
    public EdiTransactionType TransactionType { get; private set; }
    public int TotalSegments { get; private set; }
    public int TotalTransactions { get; private set; }
    public bool IsValid { get; private set; }
    public string? ValidationErrors { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    private readonly List<EdiSegment> _segments = new();
    public IReadOnlyCollection<EdiSegment> Segments => _segments.AsReadOnly();

    private EdiDocument() { }

    public static EdiDocument Create(Guid fileId, EdiTransactionType transactionType, EdiTransactionSet transactionSet, string correlationId)
    {
        return new EdiDocument
        {
            FileId = fileId,
            TransactionType = transactionType,
            TransactionSet = transactionSet,
            CorrelationId = correlationId,
            IsValid = true
        };
    }

    public void AddSegment(EdiSegment segment)
    {
        _segments.Add(segment);
        TotalSegments = _segments.Count;
        IncrementVersion();
    }

    public void SetValidationResult(bool isValid, string? errors = null)
    {
        IsValid = isValid;
        ValidationErrors = errors;
        IncrementVersion();
    }

    public void SetTransactionCount(int count)
    {
        TotalTransactions = count;
        IncrementVersion();
    }
}
