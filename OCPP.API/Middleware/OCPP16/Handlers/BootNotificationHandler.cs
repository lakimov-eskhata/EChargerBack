using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class BootNotificationHandler : IMessageHandler
    {
        private readonly ILogger<BootNotificationHandler> _logger;
        private readonly IChargePointRepository _chargePointRepository;
        private readonly WebSocketConnectionManager _connectionManager;
        
        public BootNotificationHandler(
            ILogger<BootNotificationHandler> logger,
            IChargePointRepository chargePointRepository,
            WebSocketConnectionManager connectionManager)
        {
            _logger = logger;
            _chargePointRepository = chargePointRepository;
            _connectionManager = connectionManager;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogInformation($"[OCPP1.6] BootNotification from {chargePointId}");
            
            // 1. Парсим специфичные для OCPP 1.6 данные
            var bootNotification = ParseBootNotification(message);
            
            // 2. Проверяем/создаем зарядную станцию в БД
            var chargePoint = await _chargePointRepository.GetOrCreateAsync(chargePointId, new
            {
                ChargePointId = chargePointId,
                ProtocolVersion = "1.6",
                Vendor = bootNotification.Vendor,
                Model = bootNotification.Model,
                SerialNumber = bootNotification.SerialNumber,
                FirmwareVersion = bootNotification.FirmwareVersion,
                LastBootTime = DateTime.UtcNow,
                Status = "Online",
                HeartbeatInterval = bootNotification.HeartbeatInterval
            });
            
            // 3. Обновляем в ConnectionManager
            // (в реальном коде это делается в Middleware)
            
            // 4. Возвращаем ответ для OCPP 1.6
            return new
            {
                Status = "Accepted", // или "Pending", "Rejected"
                CurrentTime = DateTime.UtcNow.ToString("o"),
                Interval = bootNotification.HeartbeatInterval
            };
        }
        
        private BootNotificationRequest ParseBootNotification(object message)
        {
            // OCPP 1.6 формат: [2, "unique-id", "BootNotification", {...}]
            var jsonArray = message as JsonArray;
            if (jsonArray == null || jsonArray.Count < 4)
                throw new InvalidOperationException("Invalid BootNotification format");
            
            var payload = jsonArray[3].Deserialize<BootNotificationRequest>();
            return payload;
        }
        
        private class BootNotificationRequest
        {
            [JsonPropertyName("chargePointVendor")]
            public string Vendor { get; set; }
            
            [JsonPropertyName("chargePointModel")]
            public string Model { get; set; }
            
            [JsonPropertyName("chargePointSerialNumber")]
            public string SerialNumber { get; set; }
            
            [JsonPropertyName("chargeBoxSerialNumber")]
            public string ChargeBoxSerialNumber { get; set; }
            
            [JsonPropertyName("firmwareVersion")]
            public string FirmwareVersion { get; set; }
            
            [JsonPropertyName("iccid")]
            public string Iccid { get; set; }
            
            [JsonPropertyName("imsi")]
            public string Imsi { get; set; }
            
            [JsonPropertyName("meterType")]
            public string MeterType { get; set; }
            
            [JsonPropertyName("meterSerialNumber")]
            public string MeterSerialNumber { get; set; }
            
            [JsonPropertyName("heartbeatInterval")]
            public int HeartbeatInterval { get; set; }
        }
    }