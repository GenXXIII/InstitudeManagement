using InstituteManagement.Application.Common.Behaviors;
using InstituteManagement.Application.Common.Validation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InstituteManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        foreach (var implementation in assembly.DefinedTypes.Where(type => !type.IsAbstract && !type.IsInterface))
        {
            foreach (var service in implementation.ImplementedInterfaces.Where(type =>
                         type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestValidator<>)))
            {
                services.AddTransient(service, implementation);
            }
        }

        return services;
    }
}
