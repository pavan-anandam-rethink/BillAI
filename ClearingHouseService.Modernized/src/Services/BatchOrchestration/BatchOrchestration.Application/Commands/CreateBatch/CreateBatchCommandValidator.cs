using FluentValidation;

namespace BatchOrchestration.Application.Commands.CreateBatch;

public class CreateBatchCommandValidator : AbstractValidator<CreateBatchCommand>
{
    public CreateBatchCommandValidator()
    {
        RuleFor(x => x.ClearinghouseId).GreaterThan(0).WithMessage("ClearinghouseId must be positive");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required");
        RuleFor(x => x.FileNames).NotEmpty().WithMessage("At least one file is required");
    }
}
