using Application.Common;
using Application.Common.Interfaces;
using OCPP.API.Middleware;
using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16;
using OCPP.API.Middleware.OCPP16.Handlers;
using OCPP.API.Middleware.OCPP20;
using OCPP.API.Middleware.OCPP21;
using OCPP.API.Middleware.OCPP21.Handlers;
using WebSocketManager = OCPP.API.Middleware.Common.WebSocketManager;

namespace OCPP.API;

public static class ServiceCollection
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрируем WebSocket и OCPP поддержку
        services.AddHttpContextAccessor();

        services.AddOCPPServices();

        services.AddSingleton<IMediatorHandler, MediatorHandler>();


        return services;
    }

    private static IServiceCollection AddOCPPServices(this IServiceCollection services)
    {
        // Фабрика middleware
        services.AddSingleton<IOCPPMiddlewareFactory, OCPPMiddlewareFactory>();

        // Общие сервисы
        services.AddSingleton<WebSocketConnectionManager>();

        // OCPP 1.6
        services.AddScoped<BootNotificationHandler>();
        services.AddScoped<AuthorizeHandler>();
        services.AddScoped<StartTransactionHandler>();
        services.AddScoped<StopTransactionHandler>();
        services.AddScoped<HeartbeatHandler>();
        services.AddScoped<StatusNotificationHandler>();
        services.AddScoped<MeterValuesHandler>();
        services.AddScoped<DataTransferHandler>();
        services.AddScoped<DiagnosticsStatusNotificationHandler>();
        services.AddScoped<FirmwareStatusNotificationHandler>();
        services.AddSingleton<OCPP16MessageProcessor>();

        // OCPP 2.0
        services.AddSingleton<OCPP20MessageProcessor>();
        // OCPP 2.0 handlers (basic)
        services.AddScoped<OCPP20.Handlers.BootNotificationHandler>();
        services.AddScoped<OCPP20.Handlers.AuthorizeHandler>();
        services.AddScoped<OCPP20.Handlers.TransactionEventHandler>();
        services.AddScoped<OCPP20.Handlers.HeartbeatHandler>();
        services.AddScoped<OCPP20.Handlers.StatusNotificationHandler>();
        services.AddScoped<OCPP20.Handlers.DataTransferHandler>();
        services.AddScoped<OCPP20.Handlers.MeterValuesHandler>();
        services.AddScoped<FirmwareStatusNotificationHandler>();

        // OCPP 2.1
        services.AddSingleton<OCPP21MessageProcessor>();
        // TODO: register 2.1 handlers as needed
        services.AddScoped<CertificateSignedHandler>();
        services.AddScoped<CustomerInformationHandler>();

        // Репозитории
        services.AddScoped<IChargePointRepository, ChargePointRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        return services;
    }
}