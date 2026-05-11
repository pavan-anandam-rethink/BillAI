using FluentValidation;

namespace SftpIngestion.Application.Commands.IngestFiles;

public class IngestFilesCommandValidator : AbstractValidator<IngestFilesCommand>
{
    public IngestFilesCommandValidator()
    {
        RuleFor(x => x.ClearinghouseId).GreaterThan(0).WithMessage("ClearinghouseId must be positive");
        RuleFor(x => x.ClearinghouseName).NotEmpty().WithMessage("ClearinghouseName is required");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required");
    }
}
