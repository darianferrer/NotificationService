using FluentValidation;

namespace NotificationService.Notifications;

public class NewNotificationValidator : AbstractValidator<NewNotification>
{
    public NewNotificationValidator()
    {
        RuleFor(c => c.From)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(c => c.To)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(c => c.Text)
            .NotEmpty();
    }
}
