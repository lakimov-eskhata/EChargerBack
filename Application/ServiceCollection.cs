using System.Reflection;
using Application.Common;
using Application.Common.Behaviors;
using Application.Common.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceCollection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMediatorHandler, MediatorHandler>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        // services.AddScoped<SieveProcessor, SieveProcessorExtension>();
        // services.Configure<SieveOptions>(_ => configuration.GetSection("Sieve"));
        
        return services;
    }
}