using System.Net.WebSockets;

namespace OCPP.API.Middleware.Common;

public class ChargePointConnection
{
    public WebSocket WebSocket { get; set; }
    public string ProtocolVersion { get; set; }
    public string ChargePointId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public string RemoteIpAddress { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
        
    public bool IsActive => WebSocket.State == WebSocketState.Open;
}