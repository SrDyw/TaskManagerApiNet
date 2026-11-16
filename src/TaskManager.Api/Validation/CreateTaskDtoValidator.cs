using FluentValidation;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos;

namespace TaskManager.Api.Validation;

public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TaskRules.TitleMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskRules.DescriptionMaxLength);

        RuleFor(x => x.State)
            .IsInEnum().WithMessage("State must be Pending, InProgress or Done.");

        RuleFor(x => x.Assignee)
            .NotEmpty().WithMessage("Assignee is required.")
            .MaximumLength(TaskRules.AssigneeMaxLength);

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("DueDate is required.")
            .Must(dueDate => UtcDateTime.Normalize(dueDate) > timeProvider.GetUtcNow().UtcDateTime)
            .WithMessage("DueDate must be in the future.");
    }
}
