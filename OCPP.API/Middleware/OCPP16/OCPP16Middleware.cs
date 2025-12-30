using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OCPP.API.Middleware.Base;
using OCPP.API.Middleware.Common;
using OCPP.API.Services;
using OCPP.API.Services.MessageProcessors;

namespace OCPP.API.Middleware.OCPP16;

public class OCPP16Middleware : BaseOCPPMiddleware
    {
        private readonly OCPP16MessageProcessor _messageProcessor;
        private readonly ICommandResponseProcessor _responseProcessor;
 
        public override string ProtocolVersion => "1.6";
        
        public OCPP16Middleware(
            ILogger<OCPP16Middleware> logger,
            IChargePointConnectionStorage connectionStorage,
            IChargePointRepository chargePointRepository,
            OCPP16MessageProcessor messageProcessor,
            ICommandResponseProcessor responseProcessor)
            : base(logger, connectionStorage, chargePointRepository)
        {
            _messageProcessor = messageProcessor;
            _responseProcessor = responseProcessor;
        }
        
        protected override async Task ProcessMessageAsync(
            string chargePointId, 
            byte[] buffer, 
            WebSocketReceiveResult result, 
            WebSocket webSocket)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
            Logger.LogDebug("[OCPP1.6] Received from {ChargePointId}: {Message}", 
                chargePointId, message.Length > 200 ? message.Substring(0, 200) + "..." : message);
            
            try
            {
                // Определяем тип сообщения
                var messageType = DetermineMessageType(message);
            
                if (messageType == "3" || messageType == "4") // CallResult или CallError
                {
                    // Обрабатываем ответ на команду сервера
                    await _responseProcessor.ProcessResponseAsync(chargePointId, message);
                }
                else
                {
                    // Обрабатываем входящие запросы от станции
                    var response = await _messageProcessor.ProcessAsync(chargePointId, message);
                
                    if (!string.IsNullOrEmpty(response))
                    {
                        await SendResponseAsync(webSocket, response);
                        Logger.LogDebug("[OCPP1.6] Response sent to {ChargePointId}", chargePointId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[OCPP1.6] Error processing message from {ChargePointId}", chargePointId);
                await SendErrorResponseAsync(webSocket, ex.Message);
            }
        }
        
        private async Task SendErrorResponseAsync(WebSocket webSocket, string errorMessage)
        {
            try
            {
                var errorResponse = new object[]
                {
                    4, // CallError
                    Guid.NewGuid().ToString(),
                    "InternalError",
                    errorMessage,
                    new { }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                await SendResponseAsync(webSocket, json);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send error response");
            }
        }
        
        protected override async Task HandleDisconnectionAsync(string chargePointId, WebSocket webSocket)
        {
            Logger.LogInformation("[OCPP1.6] Graceful disconnection for {ChargePointId}", chargePointId);
            
            await base.HandleDisconnectionAsync(chargePointId, webSocket);
        }
        
        private string DetermineMessageType(string message)
        {
            try
            {
                var json = JsonDocument.Parse(message);
                var messageTypeId = json.RootElement[0].GetInt32();
                return messageTypeId.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }
    }