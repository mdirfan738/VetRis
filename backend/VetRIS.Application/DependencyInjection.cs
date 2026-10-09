using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VetRIS.Application.Common.Behaviors;

namespace VetRIS.Application;

public static class DependencyInjection
{
    /// <summary>Registers MediatR (handlers + this assembly's notification handlers), FluentValidation validators, and the validation pipeline behavior. Called once from the Web API's Program.cs.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
