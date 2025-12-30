using System.Net.WebSockets;
using OCPP.API.Middleware.Common;

namespace Application.Interfaces;

public interface IChargePointConnectionStorage
{
    Task AddConnectionAsync(string chargePointId, WebSocket webSocket, string protocolVersion, string remoteIp);
    Task<ChargePointConnection> GetConnectionAsync(string chargePointId);
    Task RemoveConnectionAsync(string chargePointId);
    Task UpdateActivityAsync(string chargePointId);
    Task<IEnumerable<ChargePointConnection>> GetAllConnectionsAsync();
    Task<IEnumerable<ChargePointConnection>> GetConnectionsByProtocolAsync(string protocolVersion);
    Task<int> GetConnectionCountAsync();
    Task<bool> IsConnectedAsync(string chargePointId);
    Task SendMessageAsync(string chargePointId, string message);
    Task BroadcastAsync(string message, string protocolVersion = null);
}