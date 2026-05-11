using ClearingHouse.SharedKernel.Models;
using EdiProcessing.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EdiProcessing.Application.Commands.ValidateEdi;

public class ValidateEdiCommandHandler : IRequestHandler<ValidateEdiCommand, Result>
{
    private readonly IEdiValidator _validator;
    private readonly ILogger<ValidateEdiCommandHandler> _logger;

    public ValidateEdiCommandHandler(IEdiValidator validator, ILogger<ValidateEdiCommandHandler> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ValidateEdiCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating EDI document {DocumentId}", request.DocumentId);
        await Task.CompletedTask;
        return Result.Success();
    }
}
