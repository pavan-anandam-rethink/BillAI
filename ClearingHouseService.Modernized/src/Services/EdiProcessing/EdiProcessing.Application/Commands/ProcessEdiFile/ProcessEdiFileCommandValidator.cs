using FluentValidation;

namespace EdiProcessing.Application.Commands.ProcessEdiFile;

public class ProcessEdiFileCommandValidator : AbstractValidator<ProcessEdiFileCommand>
{
    public ProcessEdiFileCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().WithMessage("FileId is required");
        RuleFor(x => x.BlobUri).NotEmpty().WithMessage("BlobUri is required");
        RuleFor(x => x.FileName).NotEmpty().WithMessage("FileName is required");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required");
    }
}
