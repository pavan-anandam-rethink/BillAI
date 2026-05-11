using FluentValidation;

namespace FileTracking.Application.Commands.RecordFileEvent;

public class RecordFileEventCommandValidator : AbstractValidator<RecordFileEventCommand>
{
    public RecordFileEventCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().WithMessage("FileId is required");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required");
    }
}
