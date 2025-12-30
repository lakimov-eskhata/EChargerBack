using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Services;

public class ConnectionCleanupService : BackgroundService
    {
        private readonly IChargePointConnectionStorage _connectionStorage;
        private readonly ILogger<ConnectionCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);
        
        public ConnectionCleanupService(
            IChargePointConnectionStorage connectionStorage,
            ILogger<ConnectionCleanupService> logger)
        {
            _connectionStorage = connectionStorage;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connection Cleanup Service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    
                    var connections = await _connectionStorage.GetAllConnectionsAsync();
                    var deadConnections = connections
                        .Where(c => !c.IsActive)
                        .ToList();
                    
                    foreach (var connection in deadConnections)
                    {
                        await _connectionStorage.RemoveConnectionAsync(connection.ChargePointId);
                    }
                    
                    if (deadConnections.Any())
                    {
                        _logger.LogInformation(
                            "Cleaned up {Count} dead connections. Total active: {Active}",
                            deadConnections.Count, 
                            (await _connectionStorage.GetConnectionCountAsync()) - deadConnections.Count);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Connection Cleanup Service");
                }
            }
            
            _logger.LogInformation("Connection Cleanup Service stopped");
        }
    }