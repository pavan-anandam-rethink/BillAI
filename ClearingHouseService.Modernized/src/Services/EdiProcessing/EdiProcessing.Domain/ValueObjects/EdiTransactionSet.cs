using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;

namespace EdiProcessing.Domain.ValueObjects;

public class EdiTransactionSet : ValueObject
{
    public EdiTransactionType TransactionType { get; }
    public string ControlNumber { get; }
    public string SenderId { get; }
    public string ReceiverId { get; }

    public EdiTransactionSet(EdiTransactionType transactionType, string controlNumber, string senderId, string receiverId)
    {
        TransactionType = transactionType;
        ControlNumber = controlNumber;
        SenderId = senderId;
        ReceiverId = receiverId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TransactionType;
        yield return ControlNumber;
        yield return SenderId;
        yield return ReceiverId;
    }
}
