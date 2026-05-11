using ClearingHouse.Contracts.Dtos;
using MediatR;

namespace FileMetadata.Application.Queries.GetFileMetadata;

public record GetFileMetadataQuery : IRequest<FileMetadataDto?>
{
    public Guid FileId { get; init; }
}
