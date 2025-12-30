using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

public class BootNotificationHandler : IMessageHandler
{
    private readonly ILogger<BootNotificationHandler> _logger;
    private readonly IOCPPService _ocppService;

    public BootNotificationHandler(
        ILogger<BootNotificationHandler> logger,
        IOCPPService ocppService)
    {
        _logger = logger;
        _ocppService = ocppService;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation("Processing BootNotification for {ChargePointId}", chargePointId);

        var jsonElement = message as JsonElement?;
        if (!jsonElement.HasValue)
            throw new ArgumentException("Invalid message format");

        // Парсим данные из OCPP сообщения
        var payload = jsonElement.Value[3];

        var bootData = new BootNotificationData
        {
            ChargePointVendor = GetStringValue(payload, "chargePointVendor"),
            ChargePointModel = GetStringValue(payload, "chargePointModel"),
            ChargePointSerialNumber = GetStringValue(payload, "chargePointSerialNumber"),
            FirmwareVersion = GetStringValue(payload, "firmwareVersion"),
            Iccid = GetStringValue(payload, "iccid"),
            Imsi = GetStringValue(payload, "imsi"),
            MeterType = GetStringValue(payload, "meterType"),
            MeterSerialNumber = GetStringValue(payload, "meterSerialNumber"),
            HeartbeatInterval = GetIntValue(payload, "heartbeatInterval", 300)
        };

        // Обрабатываем через OCPPService
        var success = await _ocppService.ProcessBootNotificationAsync(chargePointId, bootData);

        // Возвращаем ответ для OCPP 1.6
        return new
        {
            status = success ? "Accepted" : "Rejected",
            currentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            interval = bootData.HeartbeatInterval
        };
    }

    private string GetStringValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private int GetIntValue(JsonElement element, string propertyName, int defaultValue)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? value.GetInt32()
            : defaultValue;
    }
}