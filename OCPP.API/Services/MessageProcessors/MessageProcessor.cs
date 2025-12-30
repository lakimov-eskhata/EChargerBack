using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;
using OCPP.API.Middleware.OCPP16;

namespace OCPP.API.Middleware.Common;

public interface IMessageProcessor
{
    Task<string> ProcessAsync(string chargePointId, string message);
    string ProtocolVersion { get; }
}

public abstract class BaseMessageProcessor : IMessageProcessor
    {
        protected readonly ILogger Logger;
        protected readonly IServiceProvider ServiceProvider;
        protected JsonSerializerOptions JsonOptions;
        
        public abstract string ProtocolVersion { get; }
        
        protected BaseMessageProcessor(
            ILogger logger,
            IServiceProvider serviceProvider)
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }
        
        public virtual async Task<string> ProcessAsync(string chargePointId, string message)
        {
            try
            {
                Logger.LogDebug($"[{ProtocolVersion}] Processing message from {chargePointId}");
                
                // 1. Парсим сообщение
                var parsedMessage = ParseMessage(message);
                
                // 2. Определяем тип сообщения
                var messageType = DetermineMessageType(parsedMessage);
                
                // 3. Получаем обработчик
                var handler = GetHandler(messageType);
                
                // 4. Выполняем обработку
                var result = await handler.HandleAsync(chargePointId, parsedMessage);
                
                // 5. Формируем ответ
                return CreateResponse(parsedMessage, result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[{ProtocolVersion}] Error processing message from {chargePointId}");
                return CreateErrorResponse(message, ex);
            }
        }
        
        protected abstract object ParseMessage(string message);
        protected abstract string DetermineMessageType(object parsedMessage);
        protected abstract IMessageHandler GetHandler(string messageType);
        protected abstract string CreateResponse(object originalMessage, object result);
        protected abstract string CreateErrorResponse(string originalMessage, Exception ex);
    }