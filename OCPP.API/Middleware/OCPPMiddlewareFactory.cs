using OCPP.API.Middleware.Base;
using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16;
using OCPP.API.Middleware.OCPP20;
using OCPP.API.Middleware.OCPP21;

namespace OCPP.API.Middleware;

public interface IOCPPMiddlewareFactory
{
    IOCPPMiddleware GetMiddleware(string protocolVersion);
}
    
public class OCPPMiddlewareFactory : IOCPPMiddlewareFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
        
    public OCPPMiddlewareFactory(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }
        
    public IOCPPMiddleware GetMiddleware(string protocolVersion)
    {
        return protocolVersion.ToLower() switch
        {
            "1.6" or "ocpp1.6" => new OCPP16Middleware(
                _loggerFactory.CreateLogger<OCPP16Middleware>(),
                _serviceProvider,
                _serviceProvider.GetRequiredService<ChargePointRegistry>(),
                _serviceProvider.GetRequiredService<IServiceScopeFactory>()),
                    
            "2.0" or "ocpp2.0" => new OCPP20Middleware(
                _loggerFactory.CreateLogger<OCPP20Middleware>(),
                _serviceProvider,
                _serviceProvider.GetRequiredService<ChargePointRegistry>(),
                _serviceProvider.GetRequiredService<OCPP20MessageProcessor>()),
                    
            "2.1" or "ocpp2.1" => new OCPP21Middleware(
                _loggerFactory.CreateLogger<OCPP21Middleware>(),
                _serviceProvider,
                _serviceProvider.GetRequiredService<ChargePointRegistry>(),
                _serviceProvider.GetRequiredService<OCPP21MessageProcessor>()),
                    
            _ => throw new ArgumentException($"Unsupported OCPP version: {protocolVersion}")
        };
    }
}