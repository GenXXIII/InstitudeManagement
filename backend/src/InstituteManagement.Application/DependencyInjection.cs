using InstituteManagement.Application.Common.Behaviors;
using InstituteManagement.Application.Common.Validation;
using InstituteManagement.Application.Features.Attendance.RecordAttendance;
using InstituteManagement.Application.Features.Grades.SubmitGrade;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InstituteManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient<IRequestValidator<RecordAttendanceCommand>, RecordAttendanceCommandValidator>();
        services.AddTransient<IRequestValidator<SubmitGradeCommand>, SubmitGradeCommandValidator>();
        return services;
    }
}
