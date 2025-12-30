using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;
using OCPP.API.Middleware.Common;
using OCPP.API.Services.Handlers.OCPP16;

namespace OCPP.API.Services.MessageProcessors;

 public class OCPP16MessageProcessor : BaseMessageProcessor
    {
        private readonly Dictionary<string, Type> _handlerTypes;
        
        public override string ProtocolVersion => "1.6";
        
        public OCPP16MessageProcessor(
            ILogger<OCPP16MessageProcessor> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _handlerTypes = new Dictionary<string, Type>
            {
                ["BootNotification"] = typeof(BootNotificationHandler),
                ["Authorize"] = typeof(AuthorizeHandler),
                ["StartTransaction"] = typeof(StartTransactionHandler),
                ["StopTransaction"] = typeof(StopTransactionHandler),
                ["Heartbeat"] = typeof(HeartbeatHandler),
                ["StatusNotification"] = typeof(StatusNotificationHandler),
                ["MeterValues"] = typeof(MeterValuesHandler),
                ["DataTransfer"] = typeof(DataTransferHandler)
            };
        }
        
        protected override object ParseMessage(string message)
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to parse OCPP 1.6 message");
                throw;
            }
        }
        
        protected override string DetermineMessageType(object parsedMessage)
        {
            var jsonElement = parsedMessage as JsonElement?;
            if (!jsonElement.HasValue)
                return "Unknown";
                
            try
            {
                var messageTypeId = jsonElement.Value[0].GetInt32();
                if (messageTypeId == 2) // Call
                {
                    return jsonElement.Value[2].GetString() ?? "Unknown";
                }
                
                return messageTypeId switch
                {
                    3 => "CallResult",
                    4 => "CallError",
                    _ => "Unknown"
                };
            }
            catch
            {
                return "Unknown";
            }
        }
        
        protected override IMessageHandler GetHandler(string messageType)
        {
            if (!_handlerTypes.TryGetValue(messageType, out var handlerType))
                throw new NotSupportedException($"Handler for {messageType} not found");
            
            return ServiceProvider.GetRequiredService(handlerType) as IMessageHandler;
        }
        
        protected override string CreateResponse(object originalMessage, object result)
        {
            var jsonElement = originalMessage as JsonElement?;
            if (!jsonElement.HasValue)
                return "[]";
            
            try
            {
                var messageId = jsonElement.Value[1].GetString();
                var response = new object[]
                {
                    3, // CallResult
                    messageId,
                    result
                };
                
                return JsonSerializer.Serialize(response);
            }
            catch
            {
                return "[]";
            }
        }
        
        protected override string CreateErrorResponse(string originalMessage, Exception ex)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(originalMessage);
                var messageId = jsonElement[1].GetString();
                
                var errorResponse = new object[]
                {
                    4, // CallError
                    messageId,
                    "InternalError",
                    ex.Message,
                    new { }
                };
                
                return JsonSerializer.Serialize(errorResponse);
            }
            catch
            {
                return JsonSerializer.Serialize(new object[]
                {
                    4,
                    "unknown",
                    "InternalError",
                    ex.Message,
                    new { }
                });
            }
        }
    }
