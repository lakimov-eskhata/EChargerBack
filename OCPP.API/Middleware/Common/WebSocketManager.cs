using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Middleware.Common;

public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly ConcurrentDictionary<string, string> _protocols = new();
        private readonly ILogger<WebSocketConnectionManager> _logger;
        
        public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
        {
            _logger = logger;
        }
        
        public void AddSocket(string chargePointId, WebSocket socket, string protocolVersion)
        {
            _sockets[chargePointId] = socket;
            _protocols[chargePointId] = protocolVersion;
            
            _logger.LogInformation($"Socket added for {chargePointId} (v{protocolVersion}). Total: {_sockets.Count}");
        }
        
        public WebSocket GetSocket(string chargePointId)
        {
            _sockets.TryGetValue(chargePointId, out var socket);
            return socket;
        }
        
        public async Task RemoveSocketAsync(string chargePointId)
        {
            if (_sockets.TryRemove(chargePointId, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection removed",
                        CancellationToken.None);
                }
                
                _protocols.TryRemove(chargePointId, out _);
                _logger.LogInformation($"Socket removed for {chargePointId}. Total: {_sockets.Count}");
            }
        }
        
        public IEnumerable<string> GetAllChargePointIds()
        {
            return _sockets.Keys;
        }
        
        public Dictionary<string, string> GetAllConnections()
        {
            return _protocols.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        public bool IsConnected(string chargePointId)
        {
            return _sockets.ContainsKey(chargePointId) && 
                   _sockets[chargePointId].State == WebSocketState.Open;
        }
        
        public async Task SendMessageAsync(string chargePointId, string message)
        {
            if (_sockets.TryGetValue(chargePointId, out var socket) && 
                socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                
                _logger.LogDebug($"Message sent to {chargePointId}");
            }
            else
            {
                _logger.LogWarning($"Cannot send message to {chargePointId}: socket not found or closed");
            }
        }
        
        public async Task BroadcastAsync(string message, string protocolVersion = null)
        {
            var tasks = new List<Task>();
            
            foreach (var kvp in _sockets)
            {
                if (protocolVersion == null || 
                    (_protocols.TryGetValue(kvp.Key, out var version) && version == protocolVersion))
                {
                    tasks.Add(SendMessageAsync(kvp.Key, message));
                }
            }
            
            await Task.WhenAll(tasks);
            _logger.LogInformation($"Broadcasted to {tasks.Count} charge points");
        }
    }