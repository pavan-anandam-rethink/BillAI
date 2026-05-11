using FluentValidation;

namespace BlobManagement.Application.Commands.UploadBlob;

public class UploadBlobCommandValidator : AbstractValidator<UploadBlobCommand>
{
    public UploadBlobCommandValidator()
    {
        RuleFor(x => x.ContainerName).NotEmpty().WithMessage("ContainerName is required");
        RuleFor(x => x.BlobName).NotEmpty().WithMessage("BlobName is required");
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).WithMessage("FileSizeBytes must be positive");
    }
}
