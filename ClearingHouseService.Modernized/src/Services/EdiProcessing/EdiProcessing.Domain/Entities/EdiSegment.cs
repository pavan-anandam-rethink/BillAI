using ClearingHouse.SharedKernel.Domain;

namespace EdiProcessing.Domain.Entities;

public class EdiSegment : Entity
{
    public Guid DocumentId { get; private set; }
    public string SegmentId { get; private set; } = string.Empty;
    public int SequenceNumber { get; private set; }
    public string RawContent { get; private set; } = string.Empty;
    public IDictionary<string, string> Elements { get; private set; } = new Dictionary<string, string>();

    private EdiSegment() { }

    public static EdiSegment Create(Guid documentId, string segmentId, int sequenceNumber, string rawContent)
    {
        return new EdiSegment
        {
            DocumentId = documentId,
            SegmentId = segmentId,
            SequenceNumber = sequenceNumber,
            RawContent = rawContent
        };
    }

    public void SetElements(IDictionary<string, string> elements)
    {
        Elements = elements;
    }
}
