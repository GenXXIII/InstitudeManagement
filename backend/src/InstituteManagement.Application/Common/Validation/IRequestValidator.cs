namespace InstituteManagement.Application.Common.Validation;

public interface IRequestValidator<in TRequest>
{
    IEnumerable<ValidationError> Validate(TRequest request);
}
