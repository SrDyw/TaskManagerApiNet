using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace TaskManager.Api.Validation;

/// <summary>
/// Runs the registered FluentValidation validator for every action argument
/// and short-circuits with a 400 ValidationProblem when any rule fails.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider, ProblemDetailsFactory problemDetailsFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            // Use the framework factory so the body matches built-in 400s (type, title, traceId).
            var problem = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest);
            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
