using ClearingHouse.SharedKernel.Domain;
using MediatR;

namespace SftpIngestion.Application.Commands;

public record PollClearinghouseCommand(string ClearinghouseId, string CorrelationId) : IRequest<Result>;
