using System.Net.WebSockets;
using Application.Common;

namespace OCPP.API.Middleware.Base;

public interface IOCPPMiddleware
{
    Task ProcessWebSocketAsync(HttpContext context, WebSocket webSocket);
    string ProtocolVersion { get; }
}

public abstract class BaseOCPPMiddleware : IOCPPMiddleware
{
    protected readonly ILogger Logger;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ChargePointConnectionStorage ConnectionStorage;

    public abstract string ProtocolVersion { get; }

    protected BaseOCPPMiddleware(
        ILogger logger,
        IServiceProvider serviceProvider, ChargePointConnectionStorage connectionStorage)
    {
        Logger = logger;
        ServiceProvider = serviceProvider;
        ConnectionStorage = connectionStorage;
    }

    public virtual async Task ProcessWebSocketAsync(HttpContext context, WebSocket webSocket)
    {
        var chargePointId = GetChargePointId(context);
        var buffer = new byte[1024 * 4]; // 4KB буфер

        Logger.LogInformation($"New {ProtocolVersion} connection from {chargePointId}");

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleDisconnection(chargePointId, webSocket);
                    break;
                }

                // Обработка сообщения
                await ProcessMessageAsync(chargePointId, buffer, result, webSocket);

                Array.Clear(buffer, 0, buffer.Length);
            }
        }
        catch (WebSocketException ex)
        {
            Logger.LogWarning(ex, $"WebSocket error for {chargePointId}");
        }
        finally
        {
            await CleanupConnection(chargePointId);
        }
    }

    protected abstract string GetChargePointId(HttpContext context);

    protected abstract Task ProcessMessageAsync(
        string chargePointId,
        byte[] buffer,
        WebSocketReceiveResult result,
        WebSocket webSocket);

    protected abstract Task HandleDisconnection(string chargePointId, WebSocket webSocket);
    protected abstract Task CleanupConnection(string chargePointId);
}