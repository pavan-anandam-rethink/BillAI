using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace BlobManagement.Application.Commands.ArchiveBlob;

public record ArchiveBlobCommand : IRequest<Result>
{
    public Guid BlobFileId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
