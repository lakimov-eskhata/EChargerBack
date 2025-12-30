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
using OCPP.API.Services;
using OCPP.API.Services.MessageProcessors;

namespace OCPP.API.Middleware.OCPP20;

public class OCPP20Middleware : BaseOCPPMiddleware
    {
        private readonly OCPP20MessageProcessor _messageProcessor;
        private readonly ICommandResponseProcessor _responseProcessor;

        public override string ProtocolVersion => "2.0";
        
        public OCPP20Middleware(
            ILogger<OCPP20Middleware> logger,
            IChargePointConnectionStorage connectionStorage,
            IChargePointRepository chargePointRepository,
            OCPP20MessageProcessor messageProcessor,
            ICommandResponseProcessor responseProcessor)
            : base(logger, connectionStorage, chargePointRepository)
        {
            _messageProcessor = messageProcessor;
            _responseProcessor = responseProcessor;
        }
        
        protected override async Task<string> GetChargePointIdAsync(HttpContext context)
        {
            // Для OCPP 2.0 получаем из URL path или headers
            var chargePointId = context.Request.Headers["X-ChargePoint-Id"].ToString();
            
            if (string.IsNullOrEmpty(chargePointId))
            {
                // Из пути: /ocpp/2.0/CP001
                var path = context.Request.Path.Value;
                if (!string.IsNullOrEmpty(path))
                {
                    var segments = path.Split('/');
                    if (segments.Length > 3)
                    {
                        chargePointId = segments[3];
                    }
                }
            }
            
            if (string.IsNullOrEmpty(chargePointId))
            {
                throw new ArgumentException("Charge point ID not found in request");
            }
            
            return chargePointId;
        }
        
        protected override async Task ProcessMessageAsync(
            string chargePointId, 
            byte[] buffer, 
            WebSocketReceiveResult result, 
            WebSocket webSocket)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
            Logger.LogDebug("[OCPP2.0] Received from {ChargePointId}: {Message}", 
                chargePointId, message.Length > 200 ? message.Substring(0, 200) + "..." : message);
            
            try
            {
                // Проверяем, является ли сообщение ответом на команду сервера (JSON-RPC response)
                if (IsJsonRpcResponse(message))
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
                        Logger.LogDebug("[OCPP2.0] Response sent to {ChargePointId}", chargePointId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[OCPP2.0] Error processing message from {ChargePointId}", chargePointId);
                await SendErrorResponseAsync(webSocket, ex.Message);
            }
        }
        
        private bool IsJsonRpcResponse(string message)
        {
            try
            {
                var json = JsonDocument.Parse(message);
                return json.RootElement.TryGetProperty("id", out _) && 
                       (json.RootElement.TryGetProperty("result", out _) || 
                        json.RootElement.TryGetProperty("error", out _));
            }
            catch
            {
                return false;
            }
        }
        
        private async Task SendErrorResponseAsync(WebSocket webSocket, string errorMessage)
        {
            try
            {
                var errorResponse = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    error = new
                    {
                        code = -32000,
                        message = errorMessage,
                        data = new { timestamp = DateTime.UtcNow }
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                await SendResponseAsync(webSocket, json);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send error response");
            }
        }
    }