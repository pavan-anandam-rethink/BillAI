using ClearingHouse.SharedKernel.Domain;

namespace EdiProcessing.Domain.Entities;

public class ProcessingResult : AggregateRoot
{
    public Guid FileId { get; private set; }
    public Guid DocumentId { get; private set; }
    public int TotalRecords { get; private set; }
    public int SuccessfulRecords { get; private set; }
    public int FailedRecords { get; private set; }
    public TimeSpan ProcessingDuration { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public IList<string> Errors { get; private set; } = new List<string>();

    private ProcessingResult() { }

    public static ProcessingResult Create(Guid fileId, Guid documentId, string correlationId)
    {
        return new ProcessingResult
        {
            FileId = fileId,
            DocumentId = documentId,
            CorrelationId = correlationId
        };
    }

    public void RecordCompletion(int totalRecords, int successfulRecords, int failedRecords, TimeSpan duration, IList<string>? errors = null)
    {
        TotalRecords = totalRecords;
        SuccessfulRecords = successfulRecords;
        FailedRecords = failedRecords;
        ProcessingDuration = duration;
        Errors = errors ?? new List<string>();
        IncrementVersion();
    }
}
