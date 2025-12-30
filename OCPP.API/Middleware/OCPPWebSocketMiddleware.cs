using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Middleware;

public class OCPPWebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOCPPMiddlewareFactory _middlewareFactory;
        private readonly ILogger<OCPPWebSocketMiddleware> _logger;
        
        public OCPPWebSocketMiddleware(
            RequestDelegate next,
            IOCPPMiddlewareFactory middlewareFactory,
            ILogger<OCPPWebSocketMiddleware> logger)
        {
            _next = next;
            _middlewareFactory = middlewareFactory;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            if (IsOCPPWebSocketRequest(context))
            {
                await HandleOCPPWebSocketAsync(context);
            }
            else
            {
                await _next(context);
            }
        }
        
        private bool IsOCPPWebSocketRequest(HttpContext context)
        {
            return context.WebSockets.IsWebSocketRequest &&
                   context.Request.Path.StartsWithSegments("/ocpp");
        }
        
        private async Task HandleOCPPWebSocketAsync(HttpContext context)
        {
            _logger.LogDebug(
                "OCPP WebSocket request: {Path}, Subprotocols: {Subprotocols}",
                context.Request.Path,
                string.Join(", ", context.WebSockets.WebSocketRequestedProtocols ?? Array.Empty<string>()));
            
            try
            {
                // Создаем middleware через фабрику
                // var middlewareFactory = context.RequestServices.GetRequiredService<IOCPPMiddlewareFactory>();
                
                var middleware = await _middlewareFactory.CreateAsync(context);

                // Принимаем WebSocket соединение с указанием поддерживаемых протоколов
                var webSocket = await context.WebSockets.AcceptWebSocketAsync(middleware.ProtocolVersion);
                
                // Обрабатываем соединение
                await middleware.ProcessWebSocketAsync(context, webSocket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling OCPP WebSocket connection");
                
                if (context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
        }
    }
    
    // Extension method для регистрации
    public static class OCPPWebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseOCPPWebSockets(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OCPPWebSocketMiddleware>();
        }
    }