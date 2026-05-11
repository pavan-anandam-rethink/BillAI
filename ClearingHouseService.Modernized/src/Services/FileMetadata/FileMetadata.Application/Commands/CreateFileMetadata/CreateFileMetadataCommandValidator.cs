using FluentValidation;

namespace FileMetadata.Application.Commands.CreateFileMetadata;

public class CreateFileMetadataCommandValidator : AbstractValidator<CreateFileMetadataCommand>
{
    public CreateFileMetadataCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage("FileName is required");
        RuleFor(x => x.BlobUri).NotEmpty().WithMessage("BlobUri is required");
        RuleFor(x => x.ClearinghouseId).GreaterThan(0).WithMessage("ClearinghouseId must be positive");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required");
    }
}
