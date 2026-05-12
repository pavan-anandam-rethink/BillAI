using ClearingHouse.EdiProcessing.Application.Commands;
using FluentValidation;

namespace ClearingHouse.EdiProcessing.Application.Validators;

/// <summary>
/// Validates <see cref="ProcessEdiFileCommand"/> ensuring all required fields are present and well-formed.
/// </summary>
public sealed class ProcessEdiFileCommandValidator : AbstractValidator<ProcessEdiFileCommand>
{
    public ProcessEdiFileCommandValidator()
    {
        RuleFor(x => x.FileReference)
            .NotNull()
            .WithMessage("FileReference is required");

        RuleFor(x => x.FileReference.FileName)
            .NotEmpty()
            .WithMessage("FileReference.FileName must not be empty")
            .When(x => x.FileReference is not null);

        RuleFor(x => x.FileReference.ContainerName)
            .NotEmpty()
            .WithMessage("FileReference.ContainerName must not be empty")
            .When(x => x.FileReference is not null);

        RuleFor(x => x.EdiTransactionType)
            .NotNull()
            .WithMessage("EdiTransactionType is required");

        RuleFor(x => x.CorrelationId)
            .NotNull()
            .WithMessage("CorrelationId is required");
    }
}
