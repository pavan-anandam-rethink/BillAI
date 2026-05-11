using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace SftpIngestion.Application.Commands.IngestFiles;

public record IngestFilesCommand : IRequest<Result>
{
    public int ClearinghouseId { get; init; }
    public string ClearinghouseName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
}
