using FluentValidation;

namespace Notification.Application.Commands.SendNotification;

public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");
        RuleFor(x => x.Message).NotEmpty().WithMessage("Message is required");
        RuleFor(x => x.Severity).NotEmpty().WithMessage("Severity is required");
    }
}
