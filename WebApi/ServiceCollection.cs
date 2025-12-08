using Application.Common;
using Application.Common.Interfaces;
using Ocpp16;

namespace WebApi;

public static class ServiceCollection
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрируем WebSocket и OCPP поддержку
        services.AddHttpContextAccessor();
        
        // Add OCPP handlers
        services.AddScoped<Ocpp16Dispatcher>();
        // services.AddScoped<Ocpp20Dispatcher>();

        
        services.AddSingleton<IMediatorHandler, MediatorHandler>();

        return services;
    }
}
