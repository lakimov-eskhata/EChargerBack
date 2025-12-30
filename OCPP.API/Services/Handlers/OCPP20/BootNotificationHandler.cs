using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

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
            _logger.LogInformation("Processing OCPP 2.0 BootNotification for {ChargePointId}", chargePointId);
            
            var jsonRpc = message as JsonRpcMessage;
            if (jsonRpc == null)
                throw new ArgumentException("Invalid message format");
            
            var request = jsonRpc.Params.Deserialize<BootNotificationRequest>();
            
            var bootData = new BootNotificationData
            {
                ChargePointVendor = request.ChargePointVendor,
                ChargePointModel = request.ChargePointModel,
                ChargePointSerialNumber = request.ChargePointSerialNumber,
                FirmwareVersion = request.FirmwareVersion,
                Iccid = request.Iccid,
                Imsi = request.Imsi,
                MeterType = request.MeterType,
                MeterSerialNumber = request.MeterSerialNumber,
                HeartbeatInterval = request.HeartbeatInterval
            };
            
            var success = await _ocppService.ProcessBootNotificationAsync(chargePointId, bootData);
            
            return new BootNotificationResponse
            {
                CurrentTime = DateTime.UtcNow,
                Interval = request.HeartbeatInterval,
                Status = success ? "Accepted" : "Rejected",
                StatusInfo = new StatusInfo
                {
                    ReasonCode = success ? "OK" : "ConnectionError",
                    AdditionalInfo = success ? "Boot accepted" : "Failed to process boot notification"
                }
            };
        }
        
        private class BootNotificationRequest
        {
            public string ChargePointVendor { get; set; } = string.Empty;
            public string ChargePointModel { get; set; } = string.Empty;
            public string ChargePointSerialNumber { get; set; } = string.Empty;
            public string FirmwareVersion { get; set; } = string.Empty;
            public string? Iccid { get; set; }
            public string? Imsi { get; set; }
            public string? MeterType { get; set; }
            public string? MeterSerialNumber { get; set; }
            public int HeartbeatInterval { get; set; } = 300;
        }
        
        private class BootNotificationResponse
        {
            public DateTime CurrentTime { get; set; }
            public int Interval { get; set; }
            public string Status { get; set; } = string.Empty;
            public StatusInfo StatusInfo { get; set; } = new();
        }
        
        private class StatusInfo
        {
            public string ReasonCode { get; set; } = string.Empty;
            public string AdditionalInfo { get; set; } = string.Empty;
        }
    }