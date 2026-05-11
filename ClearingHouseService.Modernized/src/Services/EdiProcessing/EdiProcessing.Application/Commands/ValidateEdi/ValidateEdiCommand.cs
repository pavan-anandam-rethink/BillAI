using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace EdiProcessing.Application.Commands.ValidateEdi;

public record ValidateEdiCommand : IRequest<Result>
{
    public Guid DocumentId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
