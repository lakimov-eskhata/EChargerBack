using System.Text.Json;
using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16.Handlers;
using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP16;

public class OCPP16MessageProcessor : OCPP.API.Middleware.Common.BaseMessageProcessor
{
    private readonly Dictionary<string, Type> _handlerTypes;

    public override string ProtocolVersion => "1.6";

    public OCPP16MessageProcessor(ILogger<OCPP16MessageProcessor> logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        _handlerTypes = new Dictionary<string, Type>
        {
            ["BootNotification"] = typeof(BootNotificationHandler),
            ["Authorize"] = typeof(AuthorizeHandler),
            ["StartTransaction"] = typeof(BootNotificationHandler), // temporary fallback
            ["StopTransaction"] = typeof(BootNotificationHandler),
            ["Heartbeat"] = typeof(BootNotificationHandler),
            ["StatusNotification"] = typeof(BootNotificationHandler),
            ["MeterValues"] = typeof(BootNotificationHandler),
            ["DataTransfer"] = typeof(BootNotificationHandler),
            ["DiagnosticsStatusNotification"] = typeof(BootNotificationHandler),
            ["FirmwareStatusNotification"] = typeof(BootNotificationHandler)
        };
    }

    protected override object ParseMessage(string message)
    {
        // OCPP1.6 messages are JSON arrays like: [2, "id", "Action", {payload}]
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(message, JsonOptions);
            return doc;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid OCPP 1.6 message format", ex);
        }
    }

    protected override string DetermineMessageType(object parsedMessage)
    {
        var json = (JsonElement)parsedMessage;
        if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() < 3)
            return "Unknown";

        var action = json[2].GetString();
        return action ?? "Unknown";
    }

    protected override IMessageHandler GetHandler(string messageType)
    {
        if (!_handlerTypes.TryGetValue(messageType, out var type))
            throw new NotSupportedException($"Handler for {messageType} not found");

        return ServiceProvider.GetRequiredService(type) as IMessageHandler;
    }

    protected override string CreateResponse(object originalMessage, object result)
    {
        // For OCPP 1.6: response is an array [3, "id", {payload}]
        var json = (JsonElement)originalMessage;
        var id = json[1].GetString();
        var payload = result ?? new { };
        var response = new object[] { 3, id, payload };
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    protected override string CreateErrorResponse(string originalMessage, Exception ex)
    {
        // Return a CallError message: [4, "", "InternalError", "desc", {}]
        return JsonSerializer.Serialize(new object[] { 4, "", "InternalError", ex.Message, new { } }, JsonOptions);
    }
}

