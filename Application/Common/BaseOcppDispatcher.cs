using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimpleR;
using SimpleR.Ocpp;

namespace Application.Common;

public abstract  class BaseOcppDispatcher: IWebSocketMessageDispatcher<IOcppMessage>
{
    protected readonly ILogger Logger;
    protected readonly string ProtocolVersion;
    protected readonly ChargePointConnectionStorage ConnectionStorage;
    protected readonly IHttpContextAccessor HttpContextAccessor;
    protected readonly IMediatorHandler Mediator;
    
    protected BaseOcppDispatcher(
        ILogger logger,
        IMediatorHandler mediator,
        string protocolVersion, 
        IHttpContextAccessor httpContextAccessor,
        ChargePointConnectionStorage connectionStorage)
    {
        Logger = logger;
        Mediator = mediator;
        ProtocolVersion = protocolVersion;
        ConnectionStorage = connectionStorage;
        HttpContextAccessor = httpContextAccessor;
    }

    public Task OnConnectedAsync(IWebsocketConnectionContext<IOcppMessage> connection)
    {
        try
        {
            var httpContext = HttpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                if (httpContext.Request.RouteValues.TryGetValue("chargePointId", out var idValue))
                {
                    var chargePointId = idValue?.ToString();

                    if (!string.IsNullOrEmpty(chargePointId))
                    {
                        // ConnectionStorage.Register(chargePointId, connection);
                    }
                    else
                    {
                        Logger.LogWarning("ChargePointId not found in route values for connection: {ConnectionId}", connection.ConnectionId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in OnConnectedAsync for connection: {ConnectionId}", connection.ConnectionId);
        }

        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<IOcppMessage> connection, Exception? exception)
    {
        try
        {
            var httpContext = HttpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                if (httpContext.Request.RouteValues.TryGetValue("chargePointId", out var idValue))
                {
                    var chargePointId = idValue?.ToString();

                    if (!string.IsNullOrEmpty(chargePointId))
                    {
                        ConnectionStorage.Remove(chargePointId);
                    }
                    else
                    {
                        Logger.LogWarning("ChargePointId not found in route values for connection: {ConnectionId}",
                            connection.ConnectionId);
                    }
                    // Store mapping between connection ID and station ID
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in OnDisconnectedAsync for connection: {ConnectionId}", connection.ConnectionId);
        }

        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<IOcppMessage> connection, IOcppMessage message)
    {
        Logger.LogInformation("Received OCPP message: {MessageType}", message.GetType().Name);

        switch (message)
        {
            case OcppCall call:
                await HandleCallAsync(connection, call);
                break;

            case OcppCallResult callResult:
                HandleCallResult(callResult);
                break;

            case OcppCallError callError:
                HandleCallError(callError);
                break;

            default:
                Logger.LogWarning("Unknown OCPP message type: {MessageType}", message.GetType().Name);
                break;
        }
    }
    
    public async Task SendCallResultAsync(IWebsocketConnectionContext<IOcppMessage> connection, string messageId, string response)
    {
        await connection.WriteAsync(new OcppCallResult(messageId,  response));
    }
    
    protected abstract Task HandleCallAsync(IWebsocketConnectionContext<IOcppMessage> connection, OcppCall call);
    protected abstract void HandleCallResult(OcppCallResult callResult);
    protected abstract void HandleCallError(OcppCallError callError);
}