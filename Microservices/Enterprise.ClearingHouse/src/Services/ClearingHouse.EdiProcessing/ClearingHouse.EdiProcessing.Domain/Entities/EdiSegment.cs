using ClearingHouse.EdiProcessing.Domain.Enums;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;
using ClearingHouse.SharedKernel.Domain.Entities;

namespace ClearingHouse.EdiProcessing.Domain.Entities;

/// <summary>
/// Represents a single parsed segment within an EDI file.
/// </summary>
public class EdiSegment : BaseEntity
{
    /// <summary>Gets the identifier of the parent EDI file.</summary>
    public Guid EdiFileId { get; private set; }

    /// <summary>Gets the segment identifier (e.g., ISA, GS, CLM).</summary>
    public EdiSegmentIdentifier SegmentIdentifier { get; private set; } = null!;

    /// <summary>Gets the sequence number of this segment within the file.</summary>
    public int SequenceNumber { get; private set; }

    /// <summary>Gets the raw content of the segment.</summary>
    public string RawContent { get; private set; } = string.Empty;

    /// <summary>Gets the parsed content as JSON, if available.</summary>
    public string? ParsedContent { get; private set; }

    /// <summary>Gets the validation severity result for this segment.</summary>
    public ValidationSeverity? ValidationStatus { get; private set; }

    /// <summary>Gets the list of error messages from validation.</summary>
    public List<string> ErrorMessages { get; private set; } = new();

    /// <summary>
    /// Creates a new <see cref="EdiSegment"/> instance.
    /// </summary>
    /// <param name="ediFileId">The parent EDI file identifier.</param>
    /// <param name="segmentIdentifier">The segment identifier.</param>
    /// <param name="sequenceNumber">The sequence number within the file.</param>
    /// <param name="rawContent">The raw segment content.</param>
    /// <returns>A new <see cref="EdiSegment"/> instance.</returns>
    public static EdiSegment Create(
        Guid ediFileId,
        EdiSegmentIdentifier segmentIdentifier,
        int sequenceNumber,
        string rawContent)
    {
        ArgumentNullException.ThrowIfNull(segmentIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawContent);

        return new EdiSegment
        {
            Id = Guid.NewGuid(),
            EdiFileId = ediFileId,
            SegmentIdentifier = segmentIdentifier,
            SequenceNumber = sequenceNumber,
            RawContent = rawContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets the parsed JSON content for this segment.
    /// </summary>
    /// <param name="parsedContent">The parsed content as JSON.</param>
    public void SetParsedContent(string parsedContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parsedContent);
        ParsedContent = parsedContent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the validation result for this segment.
    /// </summary>
    /// <param name="severity">The validation severity.</param>
    /// <param name="errors">Optional collection of error messages.</param>
    public void SetValidationResult(ValidationSeverity severity, IEnumerable<string>? errors = null)
    {
        ValidationStatus = severity;
        ErrorMessages = errors?.ToList() ?? new List<string>();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private EdiSegment() { }
}
