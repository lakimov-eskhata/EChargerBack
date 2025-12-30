using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class TransactionEventHandler : IMessageHandler
    {
        private readonly ILogger<TransactionEventHandler> _logger;
        private readonly IOCPPService _ocppService;
        
        public TransactionEventHandler(
            ILogger<TransactionEventHandler> logger,
            IOCPPService ocppService)
        {
            _logger = logger;
            _ocppService = ocppService;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogDebug("Processing TransactionEvent for {ChargePointId}", chargePointId);
            
            var jsonRpc = message as JsonRpcMessage;
            if (jsonRpc == null)
                throw new ArgumentException("Invalid message format");
            
            var request = jsonRpc.Params.Deserialize<TransactionEventRequest>();
            
            // Обрабатываем разные типы событий транзакций
            switch (request.EventType)
            {
                case "Started":
                    var startResult = await _ocppService.StartTransactionAsync(
                        chargePointId, 
                        request.Evse?.Id ?? 1, 
                        request.IdToken?.IdToken ?? string.Empty,
                        request.MeterValue?.FirstOrDefault()?.SampledValue?.FirstOrDefault()?.Value ?? 0);
                    
                    return new TransactionEventResponse
                    {
                        IdTokenInfo = new IdTokenInfo
                        {
                            Status = startResult.Success ? "Accepted" : "Invalid"
                        },
                        UpdatedPersonalMessage = new PersonalMessage
                        {
                            Format = "UTF8",
                            Language = "en",
                            Content = startResult.Success ? "Transaction started" : "Failed to start transaction"
                        }
                    };
                    
                case "Updated":
                    if (!string.IsNullOrEmpty(request.TransactionId))
                    {
                        await _ocppService.UpdateTransactionMeterValueAsync(
                            request.TransactionId,
                            request.MeterValue?.FirstOrDefault()?.SampledValue?.FirstOrDefault()?.Value ?? 0);
                    }
                    break;
                    
                case "Ended":
                    if (!string.IsNullOrEmpty(request.TransactionId))
                    {
                        var stopResult = await _ocppService.StopTransactionAsync(
                            request.TransactionId,
                            request.MeterValue?.LastOrDefault()?.SampledValue?.FirstOrDefault()?.Value ?? 0,
                            request.Reason);
                        
                        return new TransactionEventResponse
                        {
                            IdTokenInfo = new IdTokenInfo
                            {
                                Status = "Accepted"
                            }
                        };
                    }
                    break;
            }
            
            return new TransactionEventResponse
            {
                IdTokenInfo = new IdTokenInfo { Status = "Accepted" }
            };
        }
        
        private class TransactionEventRequest
        {
            public string EventType { get; set; } = string.Empty; // Started, Updated, Ended
            public DateTime Timestamp { get; set; }
            public string TriggerReason { get; set; } = string.Empty;
            public int SeqNo { get; set; }
            public bool Offline { get; set; }
            public int? NumberOfPhasesUsed { get; set; }
            public string? CableMaxCurrent { get; set; }
            public int ReservationId { get; set; }
            public string TransactionId { get; set; } = string.Empty;
            public IdTokenInfo? IdToken { get; set; }
            public Evse? Evse { get; set; }
            public MeterValue[]? MeterValue { get; set; }
            public string? Reason { get; set; }
        }
        
        private class TransactionEventResponse
        {
            public IdTokenInfo IdTokenInfo { get; set; } = new();
            public PersonalMessage? UpdatedPersonalMessage { get; set; }
        }
        
        private class IdTokenInfo
        {
            public string IdToken { get; set; }
            public string Status { get; set; } = string.Empty;
        }
        
        private class Evse
        {
            public int Id { get; set; }
        }
        
        private class MeterValue
        {
            public DateTime Timestamp { get; set; }
            public SampledValue[] SampledValue { get; set; } = Array.Empty<SampledValue>();
        }
        
        private class SampledValue
        {
            public double Value { get; set; }
            public string Unit { get; set; } = string.Empty;
        }
        
        private class PersonalMessage
        {
            public string Format { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }
    }