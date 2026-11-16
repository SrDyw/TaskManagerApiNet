using FluentValidation;
using TaskManager.Api.Dtos;

namespace TaskManager.Api.Validation;

public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskDtoValidator()
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

        // Past due dates are allowed on update so overdue tasks can still be edited.
        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("DueDate is required.");
    }
}
