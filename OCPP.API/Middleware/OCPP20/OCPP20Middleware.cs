using System.Net.WebSockets;
using System.Text;
using OCPP.API.Middleware.Base;
using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP20;

public partial class OCPP20Middleware : BaseOCPPMiddleware
{
    private readonly OCPP20MessageProcessor _messageProcessor;
        
    public override string ProtocolVersion => "2.0";
        
    public OCPP20Middleware(
        ILogger<OCPP20Middleware> logger,
        IServiceProvider serviceProvider,
        ChargePointRegistry chargePointRegistry,
        OCPP20MessageProcessor messageProcessor)
        : base(logger, serviceProvider, chargePointRegistry)
    {
        _messageProcessor = messageProcessor;
    }
        
    protected override async Task ProcessMessageAsync(
        string chargePointId, 
        byte[] buffer, 
        WebSocketReceiveResult result, 
        WebSocket webSocket)
    {
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
        // OCPP 2.0 использует JSON-RPC 2.0
        var response = await _messageProcessor.ProcessAsync(chargePointId, message);
            
        if (response != null)
        {
            await SendResponseAsync(webSocket, response);
        }
    }
        
    // OCPP 2.0 специфичные методы
    private async Task HandleSecurityEvent(string chargePointId, string eventMessage)
    {
        // Обработка security events для OCPP 2.0
        Logger.LogInformation($"[OCPP2.0] Security event from {chargePointId}");
    }
}