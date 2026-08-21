using InstituteManagement.Application.Common.Exceptions;
using InstituteManagement.Application.Common.Validation;
using MediatR;

namespace InstituteManagement.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IRequestValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var errors = validators
            .SelectMany(validator => validator.Validate(request))
            .GroupBy(error => error.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).Distinct().ToArray(),
                StringComparer.OrdinalIgnoreCase);

        if (errors.Count > 0)
            throw new RequestValidationException(errors);

        return next(cancellationToken);
    }
}
