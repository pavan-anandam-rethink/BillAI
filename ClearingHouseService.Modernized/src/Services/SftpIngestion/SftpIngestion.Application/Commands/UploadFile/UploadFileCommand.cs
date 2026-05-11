using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace SftpIngestion.Application.Commands.UploadFile;

public record UploadFileCommand : IRequest<Result>
{
    public string FileName { get; init; } = string.Empty;
    public string BlobUri { get; init; } = string.Empty;
    public int ClearinghouseId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
