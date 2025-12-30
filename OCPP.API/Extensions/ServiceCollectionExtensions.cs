using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using OCPP.API.Middleware;
using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16;
using OCPP.API.Middleware.OCPP20;
using OCPP.API.Services;
using OCPP.API.Services.CommandHandlers;
using OCPP.API.Services.Handlers.OCPP20;
using OCPP.API.Services.MessageProcessors;
using ocpp16 = OCPP.API.Services.Handlers.OCPP16;
using ocpp20 = OCPP.API.Services.Handlers.OCPP20;

namespace OCPP.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOCPPServices(this IServiceCollection services)
    {
        // Connection Storage
        services.AddSingleton<IChargePointConnectionStorage, ChargePointConnectionStorage>();

        // Middleware Factory
        services.AddSingleton<IOCPPMiddlewareFactory, OCPPMiddlewareFactory>();

        // Middleware
        services.AddScoped<OCPP16Middleware>();
        services.AddScoped<OCPP20Middleware>();
        // services.AddScoped<OCPP21Middleware>();

        // Repositories
        services.AddScoped<IChargePointRepository, ChargePointRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Services
        services.AddScoped<IOCPPService, OCPPService>();
        services.AddScoped<ICertificateService, CertificateService>();

        // Message Processors
        services.AddScoped<OCPP16MessageProcessor>();
        services.AddScoped<OCPP20MessageProcessor>();

        // OCPP 1.6 Handlers
        services.AddScoped<ocpp16.BootNotificationHandler>();
        services.AddScoped<ocpp16.AuthorizeHandler>();
        services.AddScoped<ocpp16.StartTransactionHandler>();
        services.AddScoped<ocpp16.StopTransactionHandler>();
        services.AddScoped<ocpp16.HeartbeatHandler>();
        services.AddScoped<ocpp16.StatusNotificationHandler>();
        services.AddScoped<ocpp16.MeterValuesHandler>();
        services.AddScoped<ocpp16.DataTransferHandler>();

        // OCPP 2.0 Handlers
        services.AddScoped<ocpp20.BootNotificationHandler>();
        services.AddScoped<ocpp20.AuthorizeHandler>();
        services.AddScoped<ocpp20.TransactionEventHandler>();
        services.AddScoped<ocpp20.HeartbeatHandler>();
        services.AddScoped<ocpp20.StatusNotificationHandler>();
        services.AddScoped<ocpp20.MeterValuesHandler>();
        services.AddScoped<ocpp20.DataTransferHandler>();
        services.AddScoped<ocpp20.SecurityEventNotificationHandler>();
        services.AddScoped<ocpp20.SignCertificateHandler>();
        services.AddScoped<ocpp20.CertificateSignedHandler>();
        
        // Background Services
        services.AddHostedService<ConnectionCleanupService>();

        // Command Handlers
        services.AddScoped<OCPP16CommandHandler>();
        services.AddScoped<OCPP20CommandHandler>();
        services.AddSingleton<ICommandHandlerFactory, CommandHandlerFactory>();
        services.AddScoped<ICommandService, CommandService>();
        
        // Command Response Processing
        services.AddSingleton<IPendingCommandsStorage, InMemoryPendingCommandsStorage>();
        services.AddScoped<ICommandResponseProcessor, CommandResponseProcessor>();
        
        return services;
    }
}