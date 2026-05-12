using ClearingHouse.SftpIngestion.Application.Commands;
using FluentValidation;

namespace ClearingHouse.SftpIngestion.Application.Validators;

/// <summary>
/// Validates the <see cref="ProcessDiscoveredFileCommand"/> before processing.
/// </summary>
public sealed class ProcessDiscoveredFileCommandValidator : AbstractValidator<ProcessDiscoveredFileCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessDiscoveredFileCommandValidator"/> class.
    /// </summary>
    public ProcessDiscoveredFileCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required.")
            .MaximumLength(500)
            .WithMessage("File name must not exceed 500 characters.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("File size must be greater than zero.");

        RuleFor(x => x.ClearinghouseIdentifier)
            .NotNull()
            .WithMessage("Clearinghouse identifier is required.");

        RuleFor(x => x.CorrelationId)
            .NotNull()
            .WithMessage("Correlation ID is required.");

        RuleFor(x => x.SftpConnectionDetails)
            .NotNull()
            .WithMessage("SFTP connection details are required.");

        RuleFor(x => x.SftpConnectionDetails.Host)
            .NotEmpty()
            .When(x => x.SftpConnectionDetails is not null)
            .WithMessage("SFTP host is required.");

        RuleFor(x => x.SftpConnectionDetails.Port)
            .InclusiveBetween(1, 65535)
            .When(x => x.SftpConnectionDetails is not null)
            .WithMessage("SFTP port must be between 1 and 65535.");

        RuleFor(x => x.IngestionJobId)
            .NotEqual(Guid.Empty)
            .WithMessage("Ingestion job ID is required.");
    }
}
