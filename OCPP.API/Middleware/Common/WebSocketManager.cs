using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;

namespace OCPP.API.Middleware.Common;

public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, ChargePointConnection> _connections = new();
        private readonly ILogger<WebSocketConnectionManager> _logger;
        private readonly Timer _cleanupTimer;
        
        public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
        {
            _logger = logger;
            
            // Таймер для очистки мертвых соединений
            _cleanupTimer = new Timer(CleanupDeadConnections, null, 
                TimeSpan.FromMinutes(5), 
                TimeSpan.FromMinutes(5));
        }
        
        public void RegisterConnection(string chargePointId, WebSocket webSocket, string protocolVersion)
        {
            var connection = new ChargePointConnection
            {
                WebSocket = webSocket,
                ProtocolVersion = protocolVersion,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true
            };
            
            _connections[chargePointId] = connection;
            
            _logger.LogInformation($"Charge point {chargePointId} (v{protocolVersion}) registered. Total connections: {_connections.Count}");
        }
        
        public async Task SendMessageAsync(string chargePointId, string message)
        {
            if (_connections.TryGetValue(chargePointId, out var connection) && 
                connection.WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await connection.WebSocket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                    
                    connection.LastActivity = DateTime.UtcNow;
                    
                    _logger.LogDebug($"Message sent to {chargePointId}");
                }
                catch (WebSocketException ex)
                {
                    _logger.LogWarning(ex, $"Failed to send message to {chargePointId}");
                    await RemoveConnectionAsync(chargePointId);
                }
            }
            else
            {
                _logger.LogWarning($"Charge point {chargePointId} not found or not connected");
            }
        }
        
        public async Task BroadcastAsync(string message, string protocolVersion = null)
        {
            var tasks = new List<Task>();
            
            foreach (var kvp in _connections)
            {
                if ((protocolVersion == null || kvp.Value.ProtocolVersion == protocolVersion) &&
                    kvp.Value.WebSocket.State == WebSocketState.Open)
                {
                    tasks.Add(SendMessageAsync(kvp.Key, message));
                }
            }
            
            await Task.WhenAll(tasks);
            _logger.LogInformation($"Broadcasted message to {tasks.Count} charge points");
        }
        
        public async Task SendCommandAsync(string chargePointId, string command, object payload)
        {
            var message = new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString(),
                method = command,
                @params = payload
            };
            
            var json = JsonConvert.SerializeObject(message);
            await SendMessageAsync(chargePointId, json);
            
            _logger.LogInformation($"Command {command} sent to {chargePointId}");
        }
        
        public async Task<bool> RemoteStartTransactionAsync(string chargePointId, int connectorId, string idTag)
        {
            var command = _connections[chargePointId].ProtocolVersion switch
            {
                "1.6" => "RemoteStartTransaction",
                "2.0" or "2.1" => "RequestStartTransaction",
                _ => throw new NotSupportedException($"Protocol version not supported")
            };

            object payload;
            switch (_connections[chargePointId].ProtocolVersion)
            {
                case "1.6":
                    payload = new { connectorId = connectorId, idTag = idTag };
                    break;
                case "2.0" or "2.1":
                    payload = new { evseId = connectorId, idToken = new { idToken = idTag, type = "ISO14443" } };
                    break;
                default:
                    throw new NotSupportedException();
            }

            await SendCommandAsync(chargePointId, command, payload);
            return true;
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
                    _logger.LogWarning(ex, $"Error while closing WebSocket for {chargePointId}");
                }
                
                _logger.LogInformation($"Charge point {chargePointId} removed. Total connections: {_connections.Count}");
            }
        }
        
        public IEnumerable<ChargePointInfo> GetConnectedChargePoints()
        {
            return _connections.Select(kvp => new ChargePointInfo
            {
                ChargePointId = kvp.Key,
                ProtocolVersion = kvp.Value.ProtocolVersion,
                ConnectedAt = kvp.Value.ConnectedAt,
                LastActivity = kvp.Value.LastActivity,
                IsActive = kvp.Value.IsActive,
                WebSocketState = kvp.Value.WebSocket.State.ToString()
            });
        }
        
        public bool IsConnected(string chargePointId)
        {
            return _connections.TryGetValue(chargePointId, out var connection) &&
                   connection.WebSocket.State == WebSocketState.Open;
        }
        
        public string GetProtocolVersion(string chargePointId)
        {
            return _connections.TryGetValue(chargePointId, out var connection) 
                ? connection.ProtocolVersion 
                : null;
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
                _logger.LogInformation($"Cleaned up {deadConnections.Count} dead connections");
            }
        }
        
        private class ChargePointConnection
        {
            public WebSocket WebSocket { get; set; }
            public string ProtocolVersion { get; set; }
            public DateTime ConnectedAt { get; set; }
            public DateTime LastActivity { get; set; }
            public bool IsActive { get; set; }
        }
        
        public class ChargePointInfo
        {
            public string ChargePointId { get; set; }
            public string ProtocolVersion { get; set; }
            public DateTime ConnectedAt { get; set; }
            public DateTime LastActivity { get; set; }
            public bool IsActive { get; set; }
            public string WebSocketState { get; set; }
        }
    }