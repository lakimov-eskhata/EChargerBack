using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Middleware.Common;



public class ChargePointConnectionStorage : IChargePointConnectionStorage
    {
        private readonly ConcurrentDictionary<string, ChargePointConnection> _connections = new();
        private readonly ILogger<ChargePointConnectionStorage> _logger;
        private readonly Timer _cleanupTimer;
        
        public ChargePointConnectionStorage(ILogger<ChargePointConnectionStorage> logger)
        {
            _logger = logger;
            
            // Очистка мертвых соединений каждые 5 минут
            _cleanupTimer = new Timer(CleanupDeadConnections, null, 
                TimeSpan.FromMinutes(5), 
                TimeSpan.FromMinutes(5));
        }
        
        public Task AddConnectionAsync(string chargePointId, WebSocket webSocket, string protocolVersion, string remoteIp)
        {
            var connection = new ChargePointConnection
            {
                ChargePointId = chargePointId,
                WebSocket = webSocket,
                ProtocolVersion = protocolVersion,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                RemoteIpAddress = remoteIp
            };
            
            _connections[chargePointId] = connection;
            
            _logger.LogInformation(
                "Charge point {ChargePointId} connected via {ProtocolVersion} from {RemoteIp}. Total: {TotalConnections}",
                chargePointId, protocolVersion, remoteIp, _connections.Count);
            
            return Task.CompletedTask;
        }
        
        public Task<ChargePointConnection> GetConnectionAsync(string chargePointId)
        {
            _connections.TryGetValue(chargePointId, out var connection);
            return Task.FromResult(connection);
        }
        
        public async Task RemoveConnectionAsync(string chargePointId)
        {
            if (_connections.TryRemove(chargePointId, out var connection))
            {
                try
                {
                    if (connection.WebSocket.State == WebSocketState.Open)
                    {
                        await connection.WebSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection removed",
                            CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while closing WebSocket for {ChargePointId}", chargePointId);
                }
                
                _logger.LogInformation(
                    "Charge point {ChargePointId} removed. Total: {TotalConnections}",
                    chargePointId, _connections.Count);
            }
        }
        
        public Task UpdateActivityAsync(string chargePointId)
        {
            if (_connections.TryGetValue(chargePointId, out var connection))
            {
                connection.LastActivity = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }
        
        public Task<IEnumerable<ChargePointConnection>> GetAllConnectionsAsync()
        {
            return Task.FromResult(_connections.Values.AsEnumerable());
        }
        
        public Task<IEnumerable<ChargePointConnection>> GetConnectionsByProtocolAsync(string protocolVersion)
        {
            var connections = _connections.Values
                .Where(c => c.ProtocolVersion == protocolVersion)
                .ToList();
                
            return Task.FromResult(connections.AsEnumerable());
        }
        
        public Task<int> GetConnectionCountAsync()
        {
            return Task.FromResult(_connections.Count);
        }
        
        public Task<bool> IsConnectedAsync(string chargePointId)
        {
            return Task.FromResult(
                _connections.TryGetValue(chargePointId, out var connection) &&
                connection.WebSocket.State == WebSocketState.Open);
        }
        
        public async Task SendMessageAsync(string chargePointId, string message)
        {
            if (!_connections.TryGetValue(chargePointId, out var connection))
            {
                _logger.LogWarning("Charge point {ChargePointId} not found", chargePointId);
                return;
            }
            
            if (connection.WebSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("WebSocket for {ChargePointId} is not open", chargePointId);
                return;
            }
            
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await connection.WebSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                
                connection.LastActivity = DateTime.UtcNow;
                
                _logger.LogDebug("Message sent to {ChargePointId}", chargePointId);
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "Failed to send message to {ChargePointId}", chargePointId);
                await RemoveConnectionAsync(chargePointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending message to {ChargePointId}", chargePointId);
            }
        }
        
        public async Task BroadcastAsync(string message, string protocolVersion = null)
        {
            var tasks = new List<Task>();
            var sentCount = 0;
            
            foreach (var connection in _connections.Values)
            {
                if (protocolVersion != null && connection.ProtocolVersion != protocolVersion)
                    continue;
                    
                if (connection.WebSocket.State == WebSocketState.Open)
                {
                    tasks.Add(SendMessageAsync(connection.ChargePointId, message));
                    sentCount++;
                }
            }
            
            await Task.WhenAll(tasks);
            _logger.LogInformation("Broadcasted message to {Count} charge points", sentCount);
        }
        
        private void CleanupDeadConnections(object state)
        {
            var deadConnections = _connections
                .Where(kvp => 
                    kvp.Value.WebSocket.State != WebSocketState.Open ||
                    (DateTime.UtcNow - kvp.Value.LastActivity).TotalMinutes > 30)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var chargePointId in deadConnections)
            {
                _ = RemoveConnectionAsync(chargePointId);
            }
            
            if (deadConnections.Any())
            {
                _logger.LogInformation("Cleaned up {Count} dead connections", deadConnections.Count);
            }
        }
        
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }