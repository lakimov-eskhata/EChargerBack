using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class DataTransferHandler : IMessageHandler
{
    private readonly ILogger<DataTransferHandler> _logger;
    private readonly IOCPPService _ocppService;
        
    public DataTransferHandler(
        ILogger<DataTransferHandler> logger,
        IOCPPService ocppService)
    {
        _logger = logger;
        _ocppService = ocppService;
    }
        
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug("Processing OCPP 2.0 DataTransfer for {ChargePointId}", chargePointId);
            
        var jsonRpc = message as JsonRpcMessage;
        if (jsonRpc == null)
            throw new ArgumentException("Invalid message format");
            
        var request = jsonRpc.Params.Deserialize<DataTransferRequest>();
            
        var result = await _ocppService.ProcessDataTransferAsync(
            chargePointId, 
            request.VendorId, 
            request.MessageId, 
            request.Data);
            
        return new DataTransferResponse
        {
            Status = result.Status,
            Data = result.Data
        };
    }
        
    private class DataTransferRequest
    {
        public string VendorId { get; set; } = string.Empty;
        public string? MessageId { get; set; }
        public string? Data { get; set; }
    }
        
    private class DataTransferResponse
    {
        public string Status { get; set; } = string.Empty; // Accepted, Rejected, UnknownMessageId, UnknownVendorId
        public string? Data { get; set; }
    }
}