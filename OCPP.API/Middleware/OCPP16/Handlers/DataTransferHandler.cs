using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class DataTransferHandler : IMessageHandler
{
    private readonly ILogger<DataTransferHandler> _logger;

    public DataTransferHandler(ILogger<DataTransferHandler> logger)
    {
        _logger = logger;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation($"[OCPP1.6] DataTransfer from {chargePointId}");
        return new { status = "Accepted", data = new { } };
    }
}

