using Application;
using Application.Common;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleR;
using SimpleR.Ocpp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Ocpp16;

public class Ocpp16Dispatcher : BaseOcppDispatcher
{
    public Ocpp16Dispatcher(
        IMediatorHandler mediator,
        ILogger<Ocpp16Dispatcher> logger,
        IHttpContextAccessor httpContextAccessor,
        ChargePointConnectionStorage connectionStorage)
        : base(logger, mediator, "1.6", httpContextAccessor, connectionStorage)
    {
    }

    protected override async Task HandleCallAsync(IWebsocketConnectionContext<IOcppMessage> connection, OcppCall call)
    {
        Logger.LogWarning(call.Action);
        try
        {
            switch (call.Action)
            {
                // case "BootNotification":
                //     var bootNotification = JsonSerializer.Deserialize<BootNotificationRequest16>(call.JsonPayload);
                //     if (bootNotification != null)
                //     {
                //         var command = new ProcessBootNotificationCommand(
                //             ChargePointId: "", //chargePointId,
                //             Request: bootNotification,
                //             MessageId: call.UniqueId,
                //             ConnectionId: connection.ConnectionId
                //         );
                //         var response = await Mediator.Send(command);
                //         await SendCallResultAsync(connection, call.UniqueId, JsonConvert.SerializeObject(response));
                //     }
                //
                //     break;

                // case "Heartbeat":
                //     var heartbeatCommand = new ProcessHeartbeatCommand
                //     {
                //         ChargePointId = chargePointId,
                //         MessageId = call.MessageId,
                //         ConnectionId = connection.ConnectionId
                //     };
                //     var heartbeatResponse = await _mediator.Send(heartbeatCommand);
                //     await SendCallResultAsync(connection, call.MessageId, heartbeatResponse);
                //     break;
                //
                // case "Authorize":
                //     var authorizeRequest = JsonSerializer.Deserialize<AuthorizeRequest16>(call.JsonPayload);
                //     if (authorizeRequest != null)
                //     {
                //         var command = new ProcessAuthorizeCommand
                //         {
                //             ChargePointId = chargePointId,
                //             Request = authorizeRequest,
                //             MessageId = call.MessageId,
                //             ConnectionId = connection.ConnectionId
                //         };
                //         var response = await _mediator.Send(command);
                //         await SendCallResultAsync(connection, call.MessageId, response);
                //     }
                //
                //     break;
                //
                // case "StartTransaction":
                //     var startTransactionRequest = JsonSerializer.Deserialize<StartTransactionRequest>(call.JsonPayload);
                //     if (startTransactionRequest != null)
                //     {
                //         var command = new ProcessStartTransactionCommand
                //         {
                //             ChargePointId = chargePointId,
                //             Request = startTransactionRequest,
                //             MessageId = call.MessageId,
                //             ConnectionId = connection.ConnectionId
                //         };
                //         var response = await _mediator.Send(command);
                //         await SendCallResultAsync(connection, call.MessageId, response);
                //     }
                //
                //     break;
                //
                // case "StopTransaction":
                //     var stopTransactionRequest = JsonSerializer.Deserialize<StopTransactionRequest>(call.JsonPayload);
                //     if (stopTransactionRequest != null)
                //     {
                //         var command = new ProcessStopTransactionCommand
                //         {
                //             ChargePointId = chargePointId,
                //             Request = stopTransactionRequest,
                //             MessageId = call.MessageId,
                //             ConnectionId = connection.ConnectionId
                //         };
                //         var response = await _mediator.Send(command);
                //         await SendCallResultAsync(connection, call.MessageId, response);
                //     }
                //
                //     break;
                //
                // case "MeterValues":
                //     var meterValuesRequest = JsonSerializer.Deserialize<MeterValuesRequest>(call.JsonPayload);
                //     if (meterValuesRequest != null)
                //     {
                //         var command = new ProcessMeterValuesCommand
                //         {
                //             ChargePointId = chargePointId,
                //             Request = meterValuesRequest,
                //             MessageId = call.MessageId,
                //             ConnectionId = connection.ConnectionId
                //         };
                //         var response = await _mediator.Send(command);
                //         await SendCallResultAsync(connection, call.MessageId, response);
                //     }
                //
                //     break;
                //
                // case "StatusNotification":
                //     var statusNotificationRequest = JsonSerializer.Deserialize<StatusNotificationRequest>(call.JsonPayload);
                //     if (statusNotificationRequest != null)
                //     {
                //         var command = new ProcessStatusNotificationCommand
                //         {
                //             ChargePointId = chargePointId,
                //             Request = statusNotificationRequest,
                //             MessageId = call.MessageId,
                //             ConnectionId = connection.ConnectionId
                //         };
                //         var response = await _mediator.Send(command);
                //         await SendCallResultAsync(connection, call.MessageId, response);
                //     }
                //
                //     break;

                default:
                    // _logger.LogWarning("Unhandled OCPP 1.6 call: {Action} from {ChargePointId}", call.Action, chargePointId);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing OCPP 1.6 call from connection: {ConnectionId}",
                connection.ConnectionId);
        }
    }

    protected override void HandleCallResult(OcppCallResult callResult)
    {
        Logger.LogInformation("Received OCPP 1.6 call result for message: {MessageId}", callResult.UniqueId);
        // Обработка результатов вызовов (если нужно)
    }

    protected override void HandleCallError(OcppCallError error)
    {
        Console.WriteLine($"[CALLERROR] ID: {error.UniqueId} — {error.ErrorCode}: {error.ErrorDescription}");
    }
}