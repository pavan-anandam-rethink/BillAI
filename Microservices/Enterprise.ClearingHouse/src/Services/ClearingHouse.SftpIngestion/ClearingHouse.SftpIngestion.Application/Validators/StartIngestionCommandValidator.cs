using ClearingHouse.SftpIngestion.Application.Commands;
using FluentValidation;

namespace ClearingHouse.SftpIngestion.Application.Validators;

/// <summary>
/// Validates the <see cref="StartIngestionCommand"/> before processing.
/// </summary>
public sealed class StartIngestionCommandValidator : AbstractValidator<StartIngestionCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartIngestionCommandValidator"/> class.
    /// </summary>
    public StartIngestionCommandValidator()
    {
        RuleFor(x => x.ClearinghouseIdentifier)
            .NotNull()
            .WithMessage("Clearinghouse identifier is required.");

        RuleFor(x => x.ClearinghouseIdentifier.Code)
            .NotEmpty()
            .When(x => x.ClearinghouseIdentifier is not null)
            .WithMessage("Clearinghouse code is required.");

        RuleFor(x => x.ClearinghouseIdentifier.Name)
            .NotEmpty()
            .When(x => x.ClearinghouseIdentifier is not null)
            .WithMessage("Clearinghouse name is required.");

        RuleFor(x => x.CorrelationId)
            .NotNull()
            .WithMessage("Correlation ID is required.");
    }
}
