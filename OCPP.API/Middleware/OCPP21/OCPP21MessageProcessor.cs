using System.Text.Json;
using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16;
using OCPP.API.Middleware.OCPP16.Handlers;

namespace OCPP.API.Middleware.OCPP21;

public class OCPP21MessageProcessor : BaseMessageProcessor
    {
        private readonly ISO15118Service _iso15118Service;
        
        public override string ProtocolVersion => "2.1";
        
        public OCPP21MessageProcessor(
            ILogger<OCPP21MessageProcessor> logger,
            IServiceProvider serviceProvider,
            ISO15118Service iso15118Service)
            : base(logger, serviceProvider)
        {
            _iso15118Service = iso15118Service;
            
            // Настройка сериализации для ISO 15118
            JsonOptions = new JsonSerializerOptions(JsonOptions)
            {
                Converters = { new ISO15118Converter() }
            };
        }
        
        protected override object ParseMessage(string message)
        {
            // OCPP 2.1 также использует JSON-RPC 2.0
            var jsonRpc = JsonSerializer.Deserialize<JsonRpcMessage>(message, JsonOptions);
            
            // Проверяем наличие ISO 15118 данных
            if (jsonRpc.Method.Contains("ISO15118"))
            {
                return ParseISO15118Message(jsonRpc);
            }
            
            return jsonRpc;
        }
        
        protected override string DetermineMessageType(object parsedMessage)
        {
            if (parsedMessage is ISO15118Message isoMessage)
                return isoMessage.MessageType;
                
            var jsonRpc = parsedMessage as JsonRpcMessage;
            return jsonRpc?.Method ?? "Unknown";
        }
        
        protected override IMessageHandler GetHandler(string messageType)
        {
            // Специальные обработчики для ISO 15118
            if (messageType.StartsWith("ISO15118"))
            {
                return ServiceProvider.GetRequiredService<ISO15118MessageHandler>();
            }
            
            // Стандартные обработчики OCPP 2.1
            var handlerType = messageType switch
            {
                "BootNotification" => typeof(BootNotificationHandler),
                "Authorize" => typeof(AuthorizeHandler),
                "TransactionEvent" => typeof(TransactionEventHandler),
                "Heartbeat" => typeof(HeartbeatHandler),
                "CertificateSigned" => typeof(CertificateSignedHandler),
                "CustomerInformation" => typeof(CustomerInformationHandler),
                "SignCertificate" => typeof(SignCertificateHandler),
                "Get15118EVCertificate" => typeof(Get15118EVCertificateHandler),
                "GetCertificateStatus" => typeof(GetCertificateStatusHandler),
                _ => throw new NotSupportedException($"Handler for {messageType} not found")
            };
            
            return ServiceProvider.GetRequiredService(handlerType) as IMessageHandler;
        }
        
        private object ParseISO15118Message(JsonRpcMessage jsonRpc)
        {
            // Парсинг ISO 15118 сообщений (плагин для электромобилей)
            return _iso15118Service.ParseMessage(jsonRpc.Params);
        }
        
        protected override string CreateResponse(object originalMessage, object result)
        {
            if (originalMessage is ISO15118Message isoMessage)
            {
                return CreateISO15118Response(isoMessage, result);
            }
            
            var request = originalMessage as JsonRpcMessage;
            
            var response = new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request?.Id,
                Result = result
            };
            
            return JsonSerializer.Serialize(response, JsonOptions);
        }
        
        private string CreateISO15118Response(ISO15118Message request, object result)
        {
            var response = new
            {
                jsonrpc = "2.0",
                id = request.Id,
                result = new
                {
                    status = "Accepted",
                    iso15118SchemaVersion = request.SchemaVersion,
                    responseData = result
                }
            };
            
            return JsonSerializer.Serialize(response, JsonOptions);
        }
        
        protected override string CreateErrorResponse(string originalMessage, Exception ex)
        {
            // Аналогично OCPP 2.0, но с ISO 15118 особенностями
            return base.CreateErrorResponse(originalMessage, ex);
        }
    }