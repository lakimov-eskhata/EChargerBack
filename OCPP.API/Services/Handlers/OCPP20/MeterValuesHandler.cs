using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Ocpp16.Messages_OCPP16;
using Domain.Entities.Station;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

 public class MeterValuesHandler : IMessageHandler
    {
        private readonly ILogger<MeterValuesHandler> _logger;
        private readonly IOCPPService _ocppService;
        private readonly ITransactionRepository _transactionRepository;
        
        public MeterValuesHandler(
            ILogger<MeterValuesHandler> logger,
            IOCPPService ocppService,
            ITransactionRepository transactionRepository)
        {
            _logger = logger;
            _ocppService = ocppService;
            _transactionRepository = transactionRepository;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogDebug("Processing OCPP 2.0 MeterValues for {ChargePointId}", chargePointId);
            
            var jsonRpc = message as JsonRpcMessage;
            if (jsonRpc == null)
                throw new ArgumentException("Invalid message format");
            
            var request = jsonRpc.Params.Deserialize<MeterValuesRequest>();
            
            foreach (var meterValue in request.MeterValue)
            {
                await ProcessMeterValue(chargePointId, request.EvseId, request.TransactionId, meterValue);
            }
            
            return new MeterValuesResponse();
        }
        
        private async Task ProcessMeterValue(string chargePointId, int evseId, string? transactionId, MeterValueData meterValue)
        {
            foreach (var sampledValue in meterValue.SampledValue)
            {
                if (!string.IsNullOrEmpty(transactionId))
                {
                    // Обновляем значение в транзакции
                    await _transactionRepository.UpdateTransactionMeterValueAsync(
                        transactionId, 
                        sampledValue.Value);
                    
                    // Сохраняем детализированные данные
                    await SaveMeterValueDetails(transactionId, meterValue, sampledValue);
                }
                
                _logger.LogTrace(
                    "Meter value for {ChargePointId}, EVSE {EvseId}: {Value} {Unit} {Measurand}",
                    chargePointId, evseId, sampledValue.Value, sampledValue.Unit, sampledValue.Measurand);
            }
        }
        
        private async Task SaveMeterValueDetails(string transactionId, MeterValueData meterValue, SampledValueData sampledValue)
        {
            var meterValueEntity = new MeterValueEntity()
            {
                Timestamp = meterValue.Timestamp,
                Value = sampledValue.Value,
                Context = sampledValue.Context,
                Format = sampledValue.Format,
                Measurand = sampledValue.Measurand,
                Phase = sampledValue.Phase,
                Location = sampledValue.Location,
                Unit = sampledValue.Unit
            };
            
            await _transactionRepository.AddMeterValueAsync(transactionId, meterValueEntity);
        }
        
        private class MeterValuesRequest
        {
            public int EvseId { get; set; }
            public string? TransactionId { get; set; }
            public MeterValueData[] MeterValue { get; set; } = Array.Empty<MeterValueData>();
        }
        
        private class MeterValueData
        {
            public DateTime Timestamp { get; set; }
            public SampledValueData[] SampledValue { get; set; } = Array.Empty<SampledValueData>();
        }
        
        private class SampledValueData
        {
            public double Value { get; set; }
            public string Context { get; set; } = string.Empty; // Sample.Periodic, Sample.Clock, etc.
            public string Format { get; set; } = string.Empty; // Raw, SignedData
            public string Measurand { get; set; } = string.Empty; // Energy.Active.Import.Register, Power.Active.Import, etc.
            public string? Phase { get; set; } // L1, L2, L3, N, L1-N
            public string? Location { get; set; } // Inlet, Outlet, Body, Cable, EV
            public string Unit { get; set; } = string.Empty; // Wh, kWh, varh, kvarh, W, kW, var, kvar, A, V, Celsius, Fahrenheit, K, Percent
            public string? SignedMeterValue { get; set; }
        }
        
        private class MeterValuesResponse
        {
            // Пустой ответ для OCPP 2.0 MeterValues
        }
    }