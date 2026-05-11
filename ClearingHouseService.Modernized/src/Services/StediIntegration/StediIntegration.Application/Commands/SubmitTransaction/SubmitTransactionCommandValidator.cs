using FluentValidation;

namespace StediIntegration.Application.Commands.SubmitTransaction;

public class SubmitTransactionCommandValidator : AbstractValidator<SubmitTransactionCommand>
{
    public SubmitTransactionCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().WithMessage("FileId is required");
        RuleFor(x => x.TransactionType).NotEmpty().WithMessage("TransactionType is required");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required");
    }
}
