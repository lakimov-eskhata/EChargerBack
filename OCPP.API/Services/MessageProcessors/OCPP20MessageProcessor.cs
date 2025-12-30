using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;
using OCPP.API.Middleware.Common;
using OCPP.API.Services.Handlers.OCPP20;

namespace OCPP.API.Services.MessageProcessors;

public class OCPP20MessageProcessor : BaseMessageProcessor
{
    private readonly Dictionary<string, Type> _handlerTypes;

    public override string ProtocolVersion => "2.0";

    public OCPP20MessageProcessor(
        ILogger<OCPP20MessageProcessor> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        // Регистрация обработчиков для OCPP 2.0
        _handlerTypes = new Dictionary<string, Type>
        {
            ["BootNotification"] = typeof(BootNotificationHandler),
            ["Authorize"] = typeof(AuthorizeHandler),
            ["TransactionEvent"] = typeof(TransactionEventHandler),
            ["Heartbeat"] = typeof(HeartbeatHandler),
            ["StatusNotification"] = typeof(StatusNotificationHandler),
            ["MeterValues"] = typeof(MeterValuesHandler),
            ["DataTransfer"] = typeof(DataTransferHandler),
            ["SecurityEventNotification"] = typeof(SecurityEventNotificationHandler),
            ["SignCertificate"] = typeof(SignCertificateHandler),
            ["CertificateSigned"] = typeof(CertificateSignedHandler)
        };

        // Настройка JSON сериализации для OCPP 2.0
        JsonOptions = new JsonSerializerOptions(JsonOptions)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    protected override object ParseMessage(string message)
    {
        try
        {
            // OCPP 2.0 использует JSON-RPC 2.0
            var jsonRpc = JsonSerializer.Deserialize<JsonRpcMessage>(message, JsonOptions);

            if (jsonRpc == null)
                throw new InvalidOperationException("Invalid JSON-RPC message");

            // Проверяем версию JSON-RPC
            if (jsonRpc.JsonRpc != "2.0")
                throw new InvalidOperationException($"Unsupported JSON-RPC version: {jsonRpc.JsonRpc}");

            return jsonRpc;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse OCPP 2.0 message");
            throw new InvalidOperationException("Invalid JSON format", ex);
        }
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

        // Формируем JSON-RPC 2.0 response
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
                    Code = -32000, // Server error
                    Message = ex.Message,
                    Data = new
                    {
                        exception = ex.GetType().Name,
                        stackTrace = ex.StackTrace,
                        timestamp = DateTime.UtcNow
                    }
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
                    code = -32700, // Parse error
                    message = "Parse error"
                }
            }, JsonOptions);
        }
    }

    // JSON-RPC структуры для OCPP 2.0
    private class JsonRpcMessage
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")] public JsonElement Params { get; set; }
    }

    private class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("result")] public object Result { get; set; } = new object();
    }

    private class JsonRpcErrorResponse
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("error")] public JsonRpcError Error { get; set; } = new();
    }

    private class JsonRpcError
    {
        [JsonPropertyName("code")] public int Code { get; set; }

        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")] public object Data { get; set; } = new object();
    }
}