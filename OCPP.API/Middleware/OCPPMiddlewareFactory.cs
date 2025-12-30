using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCPP.API.Middleware.Base;
using OCPP.API.Middleware.OCPP16;
using OCPP.API.Middleware.OCPP20;

namespace OCPP.API.Middleware;

public interface IOCPPMiddlewareFactory
{
    IOCPPMiddleware Create(string protocolVersion);
    Task<IOCPPMiddleware> CreateAsync(HttpContext context);
}

public class OCPPMiddlewareFactory : IOCPPMiddlewareFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OCPPMiddlewareFactory> _logger;

    private static readonly Dictionary<string, Type> MiddlewareTypes = new()
    {
        ["ocpp1.6"] = typeof(OCPP16Middleware),
        ["ocpp2.0"] = typeof(OCPP20Middleware),
        // ["ocpp2.1"] = typeof(OCPP21Middleware),
        ["1.6"] = typeof(OCPP16Middleware),
        ["2.0"] = typeof(OCPP20Middleware),
        // ["2.1"] = typeof(OCPP21Middleware)
    };

    public OCPPMiddlewareFactory(
        IServiceProvider serviceProvider,
        ILogger<OCPPMiddlewareFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IOCPPMiddleware Create(string protocolVersion)
    {
        var normalizedVersion = NormalizeProtocolVersion(protocolVersion);

        if (!MiddlewareTypes.TryGetValue(normalizedVersion, out var middlewareType))
        {
            _logger.LogWarning("Unsupported OCPP version: {Version}. Using default 1.6", protocolVersion);
            middlewareType = typeof(OCPP16Middleware);
        }

        using var scope = _serviceProvider.CreateScope();
        if (scope.ServiceProvider.GetRequiredService(middlewareType) is not IOCPPMiddleware middleware)
        {
            throw new InvalidOperationException($"Middleware type {middlewareType} not registered");
        }

        _logger.LogDebug("Created middleware for OCPP {Version}", normalizedVersion);
        return middleware;
    }

    public Task<IOCPPMiddleware> CreateAsync(HttpContext context)
    {
        var protocolVersion = DetermineProtocolVersion(context);
        var middleware = Create(protocolVersion);
        return Task.FromResult(middleware);
    }

    private string DetermineProtocolVersion(HttpContext context)
    {
        // 1. Получаем из WebSocket Subprotocol (приоритет 1)
        var subProtocols = context.WebSockets.WebSocketRequestedProtocols;
        if (subProtocols != null && subProtocols.Count > 0)
        {
            foreach (var protocol in subProtocols)
            {
                var normalized = NormalizeProtocolVersion(protocol);
                if (MiddlewareTypes.ContainsKey(normalized))
                {
                    _logger.LogDebug("Using WebSocket subprotocol: {Protocol}", protocol);
                    return normalized;
                }
            }
        }

        // 2. Из query string
        var queryVersion = context.Request.Query["ocppversion"].ToString();
        if (!string.IsNullOrEmpty(queryVersion))
        {
            _logger.LogDebug("Using query parameter version: {Version}", queryVersion);
            return NormalizeProtocolVersion(queryVersion);
        }

        // 3. Из headers
        var headerVersion = context.Request.Headers["OCPP-Version"].ToString();
        if (!string.IsNullOrEmpty(headerVersion))
        {
            _logger.LogDebug("Using header version: {Version}", headerVersion);
            return NormalizeProtocolVersion(headerVersion);
        }

        // 4. Из пути URL
        var path = context.Request.Path.Value ?? "";
        if (path.Contains("ocpp1.6", StringComparison.OrdinalIgnoreCase))
            return "1.6";
        if (path.Contains("ocpp2.0", StringComparison.OrdinalIgnoreCase))
            return "2.0";
        // if (path.Contains("ocpp2.1", StringComparison.OrdinalIgnoreCase))
        //     return "2.1";

        // 5. По умолчанию
        _logger.LogDebug("Using default OCPP version: 1.6");
        return "1.6";
    }

    private string NormalizeProtocolVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return "1.6";

        version = version.ToLowerInvariant().Trim();

        // Нормализация версий
        return version switch
        {
            "ocpp1.6" or "ocpp16" or "1.6" => "1.6",
            "ocpp2.0" or "ocpp20" or "2.0" => "2.0",
            // "ocpp2.1" or "ocpp21" or "2.1" => "2.1",
            _ => version
        };
    }

    public IEnumerable<string> GetSupportedVersions()
    {
        return MiddlewareTypes.Keys.Distinct();
    }
}