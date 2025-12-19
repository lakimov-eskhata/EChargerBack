using OCPP.Core.Server.Messages_OCPP20;
using Newtonsoft.Json;
using OCPP.API.Middleware.OCPP16;

namespace OCPP.API.Middleware.OCPP20.Handlers;

public class DataTransferHandler : IMessageHandler
{
    private readonly ILogger<DataTransferHandler> _logger;
    public DataTransferHandler(ILogger<DataTransferHandler> logger) { _logger = logger; }
    public Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogTrace("Processing DataTransfer (OCPP2.0)...");

        var json = (System.Text.Json.JsonElement)message;
        string payload; // assign below based on structure
        try
        {
            if (json.ValueKind == System.Text.Json.JsonValueKind.Object && json.TryGetProperty("params", out var p))
                payload = p.GetRawText();
            else if (json.ValueKind == System.Text.Json.JsonValueKind.Array && json.GetArrayLength() > 2)
                payload = json[2].GetRawText();
            else
                payload = json.GetRawText();
        }
        catch {
            payload = string.Empty;
        }

        DataTransferResponse resp = new DataTransferResponse();
        try
        {
            DataTransferRequest? req = null;
            if (!string.IsNullOrEmpty(payload))
            {
                try { req = JsonConvert.DeserializeObject<DataTransferRequest>(payload); } catch { req = null; }
            }

            // For now accept and log
            resp.Status = DataTransferStatusEnumType.Accepted;
            resp.Data = null;

            _logger.LogInformation("DataTransfer => ChargePoint={0} / VendorId={1} / MessageId={2}", chargePointId, req?.VendorId, req?.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataTransfer => Exception: {0}", ex.Message);
            resp.Status = DataTransferStatusEnumType.Rejected;
        }

        return Task.FromResult<object>(resp);
    }
}
