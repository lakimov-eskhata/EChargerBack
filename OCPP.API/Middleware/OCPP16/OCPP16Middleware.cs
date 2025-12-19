using System.Net.WebSockets;
using System.Text;
using Application.Common;
using Newtonsoft.Json;
using OCPP.API.Middleware.Base;
using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16.Handlers;

namespace OCPP.API.Middleware.OCPP16;

public partial class OCPP16Middleware : BaseOCPPMiddleware
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly OCPP16MessageProcessor _messageProcessor;
        private readonly IServiceScopeFactory _scopeFactory;

        public override string ProtocolVersion => "1.6";
        
        public OCPP16Middleware(
            ILogger<OCPP16Middleware> logger,
            IServiceProvider serviceProvider,
            IServiceScopeFactory scopeFactory,
            ChargePointConnectionStorage connectionStorage)
            : base(logger, serviceProvider, connectionStorage)
        {
            _scopeFactory = scopeFactory;

            _connectionManager = serviceProvider.GetRequiredService<WebSocketConnectionManager>();
            _messageProcessor = serviceProvider.GetService(typeof(OCPP16MessageProcessor)) as OCPP16MessageProcessor;
        }
        
        protected override string GetChargePointId(HttpContext context)
        {
            // Извлекаем ID из query string или headers
            return context.Request.Query["chargePointId"].ToString() 
                   ?? context.Request.Headers["X-ChargePoint-Id"].ToString();
        }
        
        protected override async Task ProcessMessageAsync(
            string chargePointId, 
            byte[] buffer, 
            WebSocketReceiveResult result, 
            WebSocket webSocket)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        
            // Обновляем активность в ConnectionManager
            _connectionManager.UpdateActivity(chargePointId);
        
            // Обрабатываем сообщение через MessageProcessor
            var response = await _messageProcessor.ProcessAsync(chargePointId, message);
        
            if (!string.IsNullOrEmpty(response))
            {
                // Отправляем ответ через ConnectionManager
                await _connectionManager.SendMessageAsync(chargePointId, response);
            
                // Или напрямую через WebSocket
                // await SendResponseAsync(webSocket, response);
            }
        }
        
        private async Task<string> DispatchMessageAsync(
            string chargePointId, 
            string message, 
            string messageType,
            IServiceProvider serviceProvider)
        {
            return messageType switch
            {
                "BootNotification" => await serviceProvider
                    .GetRequiredService<BootNotificationHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "Authorize" => await serviceProvider
                    .GetRequiredService<AuthorizeHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "StartTransaction" => await serviceProvider
                    .GetRequiredService<StartTransactionHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "StopTransaction" => await serviceProvider
                    .GetRequiredService<StopTransactionHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "Heartbeat" => await serviceProvider
                    .GetRequiredService<HeartbeatHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "StatusNotification" => await serviceProvider
                    .GetRequiredService<StatusNotificationHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "MeterValues" => await serviceProvider
                    .GetRequiredService<MeterValuesHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "DataTransfer" => await serviceProvider
                    .GetRequiredService<DataTransferHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "DiagnosticsStatusNotification" => await serviceProvider
                    .GetRequiredService<DiagnosticsStatusNotificationHandler>()
                    .HandleAsync(chargePointId, message),
                    
                "FirmwareStatusNotification" => await serviceProvider
                    .GetRequiredService<FirmwareStatusNotificationHandler>()
                    .HandleAsync(chargePointId, message),
                    
                _ => throw new NotSupportedException($"Message type {messageType} not supported")
            };
        }
        
        protected override async Task HandleDisconnection(string chargePointId, WebSocket webSocket)
        {
            Logger.LogInformation($"[OCPP1.6] {chargePointId} disconnected gracefully");
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Connection closed",
                CancellationToken.None);
        }
        
        protected override async Task CleanupConnection(string chargePointId)
        {
            // Удаляем из ConnectionManager
            await _connectionManager.RemoveConnectionAsync(chargePointId);
        
            // Дополнительная логика очистки
            await _chargePointRepository.UpdateStatusAsync(chargePointId, "Offline");
        
            Logger.LogInformation($"[OCPP1.6] Connection cleaned up for {chargePointId}");
        }        
        private async Task SendResponseAsync(WebSocket webSocket, string response)
        {
            var responseBytes = Encoding.UTF8.GetBytes(response);
            await webSocket.SendAsync(
                new ArraySegment<byte>(responseBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        
        private async Task SendErrorAsync(WebSocket webSocket, string chargePointId, string error)
        {
            var errorResponse = new
            {
                MessageTypeId = 4, // CallError в OCPP
                ErrorCode = "InternalError",
                ErrorDescription = error
            };
            
            await SendResponseAsync(webSocket, JsonConvert.SerializeObject(errorResponse));
        }
    }