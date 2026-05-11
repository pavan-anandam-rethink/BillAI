using ClearingHouse.Contracts.Dtos;
using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace FileMetadata.Application.Queries.SearchFiles;

public record SearchFilesQuery : IRequest<IReadOnlyList<FileMetadataDto>>
{
    public int? ClearinghouseId { get; init; }
    public string? CorrelationId { get; init; }
}
