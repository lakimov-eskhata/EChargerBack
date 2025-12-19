using System.Text.Json;
using System.Text.Json.Serialization;
using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16;
using OCPP.API.Middleware.OCPP16.Handlers;
using OCPP.API.Middleware.OCPP20.Handlers;
using OCPP.API.Middleware.OCPP21.Handlers;
using AuthorizeHandler = OCPP.API.Middleware.OCPP16.Handlers.AuthorizeHandler;
using BootNotificationHandler = OCPP.API.Middleware.OCPP16.Handlers.BootNotificationHandler;
using DataTransferHandler = OCPP.API.Middleware.OCPP16.Handlers.DataTransferHandler;
using HeartbeatHandler = OCPP.API.Middleware.OCPP16.Handlers.HeartbeatHandler;
using MeterValuesHandler = OCPP.API.Middleware.OCPP16.Handlers.MeterValuesHandler;

namespace OCPP.API.Middleware.OCPP20;

 public class OCPP20MessageProcessor : BaseMessageProcessor
    {
        private readonly Dictionary<string, Type> _handlerTypes;
        
        public override string ProtocolVersion => "2.0";
        
        public OCPP20MessageProcessor(
            ILogger<OCPP20MessageProcessor> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            // Регистрация обработчиков OCPP 2.0
            _handlerTypes = new Dictionary<string, Type>
            {
                ["BootNotification"] = typeof(BootNotificationHandler),
                ["Authorize"] = typeof(AuthorizeHandler),
                ["TransactionEvent"] = typeof(TransactionEventHandler),
                ["Heartbeat"] = typeof(HeartbeatHandler),
                ["StatusNotification"] = typeof(StatusNotificationHandler),
                ["SecurityEventNotification"] = typeof(SecurityEventNotificationHandler),
                ["DataTransfer"] = typeof(DataTransferHandler),
                ["MeterValues"] = typeof(MeterValuesHandler),
                ["FirmwareStatusNotification"] = typeof(FirmwareStatusNotificationHandler),
                ["SignCertificate"] = typeof(SignCertificateHandler),
                ["CertificateSigned"] = typeof(CertificateSignedHandler)
            };
        }
        
        protected override object ParseMessage(string message)
        {
            // OCPP 2.0 использует JSON-RPC 2.0
            var jsonRpc = JsonSerializer.Deserialize<JsonRpcMessage>(message, JsonOptions);
            
            if (jsonRpc == null)
                throw new InvalidOperationException("Invalid JSON-RPC message");
                
            return jsonRpc;
        }
        
        protected override string DetermineMessageType(object parsedMessage)
        {
            var jsonRpc = parsedMessage as JsonRpcMessage;
            return jsonRpc?.Method ?? "Unknown";
        }
        
        protected override IMessageHandler GetHandler(string messageType)
        {
            if (!_handlerTypes.TryGetValue(messageType, out var handlerType))
                throw new NotSupportedException($"Handler for {messageType} not found");
            
            return ServiceProvider.GetRequiredService(handlerType) as IMessageHandler;
        }
        
        protected override string CreateResponse(object originalMessage, object result)
        {
            var request = originalMessage as JsonRpcMessage;
            
            // Формируем JSON-RPC response
            var response = new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request?.Id,
                Result = result
            };
            
            return JsonSerializer.Serialize(response, JsonOptions);
        }
        
        protected override string CreateErrorResponse(string originalMessage, Exception ex)
        {
            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcMessage>(originalMessage, JsonOptions);
                
                var errorResponse = new JsonRpcErrorResponse
                {
                    JsonRpc = "2.0",
                    Id = request?.Id,
                    Error = new JsonRpcError
                    {
                        Code = -32000,
                        Message = ex.Message,
                        Data = ex.StackTrace
                    }
                };
                
                return JsonSerializer.Serialize(errorResponse, JsonOptions);
            }
            catch
            {
                // Если не удалось распарсить оригинальный запрос
                return JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    error = new
                    {
                        code = -32700,
                        message = "Parse error"
                    }
                }, JsonOptions);
            }
        }
        
        // JSON-RPC структуры для OCPP 2.0
        private class JsonRpcMessage
        {
            [JsonPropertyName("jsonrpc")]
            public string JsonRpc { get; set; } = "2.0";
            
            [JsonPropertyName("id")]
            public string Id { get; set; }
            
            [JsonPropertyName("method")]
            public string Method { get; set; }
            
            [JsonPropertyName("params")]
            public JsonElement Params { get; set; }
        }
        
        private class JsonRpcResponse
        {
            [JsonPropertyName("jsonrpc")]
            public string JsonRpc { get; set; } = "2.0";
            
            [JsonPropertyName("id")]
            public string Id { get; set; }
            
            [JsonPropertyName("result")]
            public object Result { get; set; }
        }
        
        private class JsonRpcErrorResponse
        {
            [JsonPropertyName("jsonrpc")]
            public string JsonRpc { get; set; } = "2.0";
            
            [JsonPropertyName("id")]
            public string Id { get; set; }
            
            [JsonPropertyName("error")]
            public JsonRpcError Error { get; set; }
        }
        
        private class JsonRpcError
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }
            
            [JsonPropertyName("message")]
            public string Message { get; set; }
            
            [JsonPropertyName("data")]
            public object Data { get; set; }
        }
    }