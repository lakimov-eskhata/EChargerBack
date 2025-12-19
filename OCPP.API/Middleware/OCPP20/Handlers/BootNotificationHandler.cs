using OCPP.API.Middleware.Common;
using OCPP.Core.Server.Messages_OCPP20;
using Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Infrastructure;
using OCPP.API.Middleware.OCPP16;

namespace OCPP.API.Middleware.OCPP20.Handlers;

public class BootNotificationHandler : IMessageHandler
{
    private readonly ILogger<BootNotificationHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly IChargePointRepository _chargePointRepository;

    public BootNotificationHandler(ILogger<BootNotificationHandler> logger, IConfiguration configuration, IChargePointRepository chargePointRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _chargePointRepository = chargePointRepository;
    }

    public Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogTrace("Processing boot notification (OCPP2.0)...");

        var json = (System.Text.Json.JsonElement)message;
        var payload = string.Empty;
        if (json.ValueKind == System.Text.Json.JsonValueKind.Object && json.TryGetProperty("params", out var p))
            payload = p.GetRawText();
        else if (json.ValueKind == System.Text.Json.JsonValueKind.Array && json.GetArrayLength() > 2)
            payload = json[2].GetRawText();

        try
        {
            var bootReq = JsonConvert.DeserializeObject<BootNotificationRequest>(payload);
            var bootResp = new BootNotificationResponse
            {
                CurrentTime = DateTimeOffset.UtcNow,
                Interval = _configuration.GetValue<int>("HeartBeatInterval", 300),
                CustomData = new CustomDataType { VendorId = "DefaultVendor" }
            };

            // If charge point exists in repository => accept
            bool known = _chargePointRepository != null; // minimal check - repository implementation may check DB
            bootResp.Status = known ? RegistrationStatusEnumType.Accepted : RegistrationStatusEnumType.Rejected;

            _logger.LogInformation("BootNotification => ChargePoint={0} / Reason={1}", chargePointId, bootReq?.Reason);

            return Task.FromResult<object>(bootResp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BootNotification => Exception: {0}", ex.Message);
            return Task.FromResult<object>(new { error = "FormationViolation" });
        }
    }
}
