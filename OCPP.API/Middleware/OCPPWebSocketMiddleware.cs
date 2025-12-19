namespace OCPP.API.Middleware;

public class OCPPWebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOCPPMiddlewareFactory _middlewareFactory;
        
    public OCPPWebSocketMiddleware(
        RequestDelegate next,
        IOCPPMiddlewareFactory middlewareFactory)
    {
        _next = next;
        _middlewareFactory = middlewareFactory;
    }
        
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest && 
            context.Request.Path.StartsWithSegments("/ocpp"))
        {
            // Определяем версию OCPP
            var protocolVersion = DetermineProtocolVersion(context);
                
            // Получаем соответствующий middleware
            var ocppMiddleware = _middlewareFactory.GetMiddleware(protocolVersion);
                
            // Принимаем WebSocket соединение
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                
            // Обрабатываем соединение
            await ocppMiddleware.ProcessWebSocketAsync(context, webSocket);
        }
        else
        {
            await _next(context);
        }
    }
        
    private string DetermineProtocolVersion(HttpContext context)
    {
        // Приоритет определения версии:
        // 1. Из query string: ?ocppVersion=2.0
        // 2. Из заголовков: OCPP-Version
        // 3. Из subprotocol WebSocket
        // 4. По умолчанию: 1.6
            
        return context.Request.Query["ocppVersion"].ToString()
               ?? context.Request.Headers["OCPP-Version"].ToString()
               ?? context.WebSockets.WebSocketRequestedProtocols?.FirstOrDefault()
               ?? "1.6";
    }
}