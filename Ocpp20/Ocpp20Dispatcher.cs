using System.Text.Json;
using Application;
using Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimpleR;
using SimpleR.Ocpp;

namespace Ocpp20;

// public class Ocpp20Dispatcher : BaseOcppDispatcher
// {
//     public Ocpp20Dispatcher(
//         IMediator mediator,
//         ILogger<Ocpp20Dispatcher> logger,
//         IHttpContextAccessor httpContextAccessor,
//         ChargePointConnectionStorage connectionStorage)
//         : base(logger, mediator, "2.0.1", httpContextAccessor, connectionStorage)
//     {
//     }
//
//     protected override async Task HandleCallAsync(IWebsocketConnectionContext<IOcppMessage> connection, OcppCall call)
//     {
//         try
//         {
//             switch (call.Action)
//             {
//                  case "BootNotification":
//                     var bootNotification = JsonSerializer.Deserialize<BootNotificationRequest20>(call.JsonPayload);
//                     if (bootNotification != null)
//                     {
//                         var command = new ProcessBootNotification20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = bootNotification,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "Heartbeat":
//                     var heartbeatCommand = new ProcessHeartbeat20Command
//                     {
//                         ChargePointId = chargePointId,
//                         MessageId = call.UniqueId,
//                         ConnectionId = connection.ConnectionId
//                     };
//                     var heartbeatResponse = await _mediator.Send(heartbeatCommand);
//                     await SendCallResultAsync(connection, call.UniqueId, heartbeatResponse);
//                     break;
//
//                 case "Authorize":
//                     var authorizeRequest = JsonSerializer.Deserialize<AuthorizeRequest20>(call.JsonPayload);
//                     if (authorizeRequest != null)
//                     {
//                         var command = new ProcessAuthorize20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = authorizeRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "TransactionEvent":
//                     var transactionEventRequest = JsonSerializer.Deserialize<TransactionEventRequest20>(call.JsonPayload);
//                     if (transactionEventRequest != null)
//                     {
//                         var command = new ProcessTransactionEvent20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = transactionEventRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "StatusNotification":
//                     var statusNotificationRequest = JsonSerializer.Deserialize<StatusNotificationRequest20>(call.JsonPayload);
//                     if (statusNotificationRequest != null)
//                     {
//                         var command = new ProcessStatusNotification20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = statusNotificationRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "MeterValues":
//                     var meterValuesRequest = JsonSerializer.Deserialize<MeterValuesRequest20>(call.JsonPayload);
//                     if (meterValuesRequest != null)
//                     {
//                         var command = new ProcessMeterValues20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = meterValuesRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "NotifyEvent":
//                     var notifyEventRequest = JsonSerializer.Deserialize<NotifyEventRequest20>(call.JsonPayload.ToString());
//                     if (notifyEventRequest != null)
//                     {
//                         var command = new ProcessNotifyEvent20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = notifyEventRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "NotifyMonitoringReport":
//                     var monitoringReportRequest = JsonSerializer.Deserialize<NotifyMonitoringReportRequest20>(call.JsonPayload.ToString());
//                     if (monitoringReportRequest != null)
//                     {
//                         var command = new ProcessNotifyMonitoringReport20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = monitoringReportRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "SecurityEventNotification":
//                     var securityEventRequest = JsonSerializer.Deserialize<SecurityEventNotificationRequest20>(call.JsonPayload.ToString());
//                     if (securityEventRequest != null)
//                     {
//                         var command = new ProcessSecurityEventNotification20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = securityEventRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "SignCertificate":
//                     var signCertificateRequest = JsonSerializer.Deserialize<SignCertificateRequest20>(call.JsonPayload.ToString());
//                     if (signCertificateRequest != null)
//                     {
//                         var command = new ProcessSignCertificate20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = signCertificateRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "LogStatusNotification":
//                     var logStatusRequest = JsonSerializer.Deserialize<LogStatusNotificationRequest20>(call.JsonPayload.ToString());
//                     if (logStatusRequest != null)
//                     {
//                         var command = new ProcessLogStatusNotification20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = logStatusRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "FirmwareStatusNotification":
//                     var firmwareStatusRequest = JsonSerializer.Deserialize<FirmwareStatusNotificationRequest20>(call.JsonPayload.ToString());
//                     if (firmwareStatusRequest != null)
//                     {
//                         var command = new ProcessFirmwareStatusNotification20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = firmwareStatusRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "PublishFirmwareStatusNotification":
//                     var publishFirmwareStatusRequest = JsonSerializer.Deserialize<PublishFirmwareStatusNotificationRequest20>(call.JsonPayload.ToString());
//                     if (publishFirmwareStatusRequest != null)
//                     {
//                         var command = new ProcessPublishFirmwareStatusNotification20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = publishFirmwareStatusRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "ReservationStatusUpdate":
//                     var reservationStatusRequest = JsonSerializer.Deserialize<ReservationStatusUpdateRequest20>(call.JsonPayload.ToString());
//                     if (reservationStatusRequest != null)
//                     {
//                         var command = new ProcessReservationStatusUpdate20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = reservationStatusRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "CustomerInformation":
//                     var customerInfoRequest = JsonSerializer.Deserialize<CustomerInformationRequest20>(call.JsonPayload.ToString());
//                     if (customerInfoRequest != null)
//                     {
//                         var command = new ProcessCustomerInformation20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = customerInfoRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//
//                 case "DataTransfer":
//                     var dataTransferRequest = JsonSerializer.Deserialize<DataTransferRequest20>(call.JsonPayload.ToString());
//                     if (dataTransferRequest != null)
//                     {
//                         var command = new ProcessDataTransfer20Command
//                         {
//                             ChargePointId = chargePointId,
//                             Request = dataTransferRequest,
//                             MessageId = call.UniqueId,
//                             ConnectionId = connection.ConnectionId
//                         };
//                         var response = await _mediator.Send(command);
//                         await SendCallResultAsync(connection, call.UniqueId, response);
//                     }
//                     break;
//                 default:
//                    
//                     break;
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error processing OCPP 2.0.1 call from connection: {ConnectionId}", connection.ConnectionId);
//         }
//     }
//
//     protected override void HandleCallResult(OcppCallResult callResult)
//     {
//         _logger.LogInformation("Received OCPP 2.0.1 call result for message: {MessageId}", callResult.MessageId);
//         // Обработка результатов вызовов
//     }
//
//     protected override void HandleCallError(OcppCallError callError)
//     {
//         _logger.LogWarning("Received OCPP 2.0.1 call error: {ErrorCode} - {ErrorDescription} for message: {MessageId}", 
//             callError.ErrorCode, callError.ErrorDescription, callError.UniqueId);
//         // Обработка ошибок вызовов
//     }
// }