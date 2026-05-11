using ClearingHouse.SharedKernel.Models;
using EdiProcessing.Domain.Entities;
using EdiProcessing.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EdiProcessing.Infrastructure.Parsers;

public class BasicEdiValidator : IEdiValidator
{
    private readonly ILogger<BasicEdiValidator> _logger;

    public BasicEdiValidator(ILogger<BasicEdiValidator> logger) => _logger = logger;

    public Task<Result> ValidateAsync(EdiDocument document, CancellationToken cancellationToken = default)
    {
        if (document.TotalSegments == 0)
        {
            document.SetValidationResult(false, "Document contains no segments");
            return Task.FromResult(Result.Failure("Document contains no segments"));
        }

        document.SetValidationResult(true);
        _logger.LogInformation("EDI document {DocumentId} validated successfully", document.Id);
        return Task.FromResult(Result.Success());
    }
}
