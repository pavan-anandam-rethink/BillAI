using ClearingHouse.Contracts.Dtos;
using MediatR;

namespace SftpIngestion.Application.Queries.GetIngestionStatus;

public record GetIngestionStatusQuery : IRequest<FileMetadataDto?>
{
    public Guid FileId { get; init; }
}
