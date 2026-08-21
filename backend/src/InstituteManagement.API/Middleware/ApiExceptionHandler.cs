using InstituteManagement.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.API.Middleware;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            RequestValidationException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled API exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(
                "API request rejected with status {StatusCode} for {Method} {Path}: {Reason}",
                status,
                context.Request.Method,
                context.Request.Path,
                exception.Message);

        context.Response.StatusCode = status;
        var problem = exception is RequestValidationException validationException
            ? new ValidationProblemDetails(validationException.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                Status = status,
                Title = "One or more validation errors occurred."
            }
            : new ProblemDetails
            {
                Status = status,
                Title = TitleFor(status),
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "The request could not be completed."
                    : exception.Message
            };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Invalid request.",
        StatusCodes.Status404NotFound => "Resource not found.",
        StatusCodes.Status409Conflict => "The request conflicts with current data.",
        _ => "An unexpected error occurred."
    };
}
