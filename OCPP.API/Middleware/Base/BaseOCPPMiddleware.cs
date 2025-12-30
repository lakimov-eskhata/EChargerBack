using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Middleware.Base;

public interface IOCPPMiddleware
{
    Task ProcessWebSocketAsync(HttpContext context, WebSocket webSocket);
    string ProtocolVersion { get; }
}

public abstract class BaseOCPPMiddleware : IOCPPMiddleware
{
    protected readonly ILogger Logger;
    protected readonly IChargePointConnectionStorage ConnectionStorage;
    protected readonly IChargePointRepository ChargePointRepository;

    public abstract string ProtocolVersion { get; }

    protected BaseOCPPMiddleware(
        ILogger logger,
        IChargePointConnectionStorage connectionStorage,
        IChargePointRepository chargePointRepository)
    {
        Logger = logger;
        ConnectionStorage = connectionStorage;
        ChargePointRepository = chargePointRepository;
    }

    public virtual async Task ProcessWebSocketAsync(HttpContext context, WebSocket webSocket)
    {
        var chargePointId = await GetChargePointIdAsync(context);
        var remoteIp = GetRemoteIpAddress(context);

        // Регистрируем соединение
        await ConnectionStorage.AddConnectionAsync(chargePointId, webSocket, ProtocolVersion, remoteIp);

        Logger.LogInformation(
            "Processing WebSocket for {ChargePointId} via {ProtocolVersion}",
            chargePointId, ProtocolVersion);

        var buffer = new byte[1024 * 16]; // 16KB buffer

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleDisconnectionAsync(chargePointId, webSocket);
                    break;
                }

                // Обновляем активность
                await ConnectionStorage.UpdateActivityAsync(chargePointId);

                // Обрабатываем сообщение
                await ProcessMessageAsync(chargePointId, buffer, result, webSocket);

                // Очищаем буфер
                Array.Clear(buffer, 0, buffer.Length);
            }
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            Logger.LogWarning("WebSocket connection closed prematurely for {ChargePointId}", chargePointId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing WebSocket for {ChargePointId}", chargePointId);
        }
        finally
        {
            await CleanupConnectionAsync(chargePointId);
        }
    }

    protected virtual Task<string> GetChargePointIdAsync(HttpContext context)
    {
        string? chargePointId = null;

        // 1. Пытаемся извлечь из пути WebSocket (например: ws://localhost:5088/ocpp/CP001)
        var path = context.Request.Path.Value;

        // Паттерн для OCPP WebSocket URL: /ocpp/{chargePointId}
        if (!string.IsNullOrEmpty(path))
        {
            var pathSegments = path.Trim('/').Split('/');

            // Ищем сегмент после "/ocpp/"
            var ocppIndex = Array.IndexOf(pathSegments, "ocpp");
            if (ocppIndex >= 0 && ocppIndex + 1 < pathSegments.Length)
            {
                chargePointId = pathSegments[ocppIndex + 1];

                // Очищаем от query параметров если есть
                var queryIndex = chargePointId.IndexOf('?');
                if (queryIndex > 0)
                {
                    chargePointId = chargePointId.Substring(0, queryIndex);
                }
            }
        }

        // 2. Если не нашли в пути, проверяем query параметр
        if (string.IsNullOrEmpty(chargePointId))
        {
            chargePointId = context.Request.Query["chargePointId"].ToString();
        }

        // 3. Проверяем заголовок
        if (string.IsNullOrEmpty(chargePointId) )
        {
            chargePointId = context.Request.Headers["X-ChargePoint-Id"].ToString();
        }

        // 4. Проверяем custom claim если есть аутентификация
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var claimChargePointId = context.User.FindFirst("charge_point_id")?.Value
                                     ?? context.User.FindFirst("client_id")?.Value;

            if (!string.IsNullOrEmpty(claimChargePointId))
            {
                chargePointId = claimChargePointId;
            }
        }

        // 5. Логирование для отладки
        if (string.IsNullOrEmpty(chargePointId))
        {
            Logger?.LogWarning("Cannot determine charge point ID from request. Path: {Path}", path);
            chargePointId = null;
        }
        else
        {
            Logger?.LogDebug("Resolved charge point ID: {ChargePointId} from path: {Path}", chargePointId, path);
        }

        return Task.FromResult(chargePointId);
    }

    protected virtual string GetRemoteIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    protected abstract Task ProcessMessageAsync(
        string chargePointId,
        byte[] buffer,
        WebSocketReceiveResult result,
        WebSocket webSocket);

    protected virtual async Task HandleDisconnectionAsync(string chargePointId, WebSocket webSocket)
    {
        Logger.LogInformation("Graceful disconnection for {ChargePointId}", chargePointId);

        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnected",
                CancellationToken.None);
        }
    }

    protected virtual async Task CleanupConnectionAsync(string chargePointId)
    {
        try
        {
            // Удаляем из хранилища
            await ConnectionStorage.RemoveConnectionAsync(chargePointId);

            // Обновляем статус в БД
            await ChargePointRepository.UpdateStatusAsync(chargePointId, "Offline");

            Logger.LogInformation("Cleaned up connection for {ChargePointId}", chargePointId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error during cleanup for {ChargePointId}", chargePointId);
        }
    }

    protected virtual async Task SendResponseAsync(WebSocket webSocket, string response)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var responseBytes = Encoding.UTF8.GetBytes(response);
            await webSocket.SendAsync(
                new ArraySegment<byte>(responseBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
}