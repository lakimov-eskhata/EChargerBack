using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.Station;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class ChargePointRepository : IChargePointRepository
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ChargePointRepository> _logger;
        
        public ChargePointRepository(
            IApplicationDbContext context,
            ILogger<ChargePointRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<ChargePointEntity?> GetByIdAsync(string chargePointId)
        {
            try
            {
                return await _context.ChargePoints.AsNoTracking()
                    .Include(cp => cp.Connectors)
                    .FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting charge point by ID: {ChargePointId}", chargePointId);
                throw;
            }
        }
        
        public async Task<IEnumerable<ChargePointEntity>> GetAllAsync()
        {
            return await _context.ChargePoints
                .Include(cp => cp.Connectors)
                .OrderBy(cp => cp.ChargePointId)
                .ToListAsync();
        }
        
        public async Task<ChargePointEntity> CreateAsync(ChargePointEntity chargePoint)
        {
            try
            {
                chargePoint.CreatedAt = DateTime.UtcNow;
                chargePoint.UpdatedAt = DateTime.UtcNow;
                
                _context.ChargePoints.Add(chargePoint);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created charge point: {ChargePointId}", chargePoint.ChargePointId);
                return chargePoint;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating charge point: {ChargePointId}", chargePoint.ChargePointId);
                throw;
            }
        }
        
        public async Task<ChargePointEntity> UpdateAsync(ChargePointEntity chargePoint)
        {
            try
            {
                chargePoint.UpdatedAt = DateTime.UtcNow;
                _context.ChargePoints.Update(chargePoint);
                await _context.SaveChangesAsync();
                
                return chargePoint;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating charge point: {ChargePointId}", chargePoint.ChargePointId);
                throw;
            }
        }
        
        public async Task<bool> DeleteAsync(string chargePointId)
        {
            try
            {
                var chargePoint = await GetByIdAsync(chargePointId);
                if (chargePoint == null)
                    return false;
                    
                _context.ChargePoints.Remove(chargePoint);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted charge point: {ChargePointId}", chargePointId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting charge point: {ChargePointId}", chargePointId);
                throw;
            }
        }
        
        public async Task<ChargePointEntity?> GetBySerialNumberAsync(string serialNumber)
        {
            return await _context.ChargePoints
                .FirstOrDefaultAsync(cp => cp.SerialNumber == serialNumber);
        }
        
        public async Task<IEnumerable<ChargePointEntity>> GetByStatusAsync(string status)
        {
            return await _context.ChargePoints
                .Where(cp => cp.Status == status)
                .Include(cp => cp.Connectors)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<ChargePointEntity>> GetByProtocolVersionAsync(string protocolVersion)
        {
            return await _context.ChargePoints
                .Where(cp => cp.ProtocolVersion == protocolVersion)
                .ToListAsync();
        }
        
        public async Task<ChargePointEntity?> GetOrCreateAsync(string chargePointId, Action<ChargePointEntity>? initialize = null)
        {
            var chargePoint = await GetByIdAsync(chargePointId);
            
            if (chargePoint == null)
            {
                chargePoint = new ChargePointEntity()
                {
                    ChargePointId = chargePointId,
                    Name = $"Charge Point {chargePointId}",
                    ProtocolVersion = "1.6",
                    Status = "Offline",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                initialize?.Invoke(chargePoint);
                chargePoint = await CreateAsync(chargePoint);
            }
            
            return chargePoint;
        }
        
        public async Task<bool> UpdateStatusAsync(string chargePointId, string status)
        {
            try
            {
                var chargePoint = await GetByIdAsync(chargePointId);
                if (chargePoint == null)
                    return false;
                    
                chargePoint.UpdateStatus(status);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated status for {ChargePointId} to {Status}", chargePointId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<bool> UpdateHeartbeatAsync(string chargePointId)
        {
            try
            {
                var chargePoint = await GetByIdAsync(chargePointId);
                if (chargePoint == null)
                    return false;
                    
                chargePoint.UpdateHeartbeat();
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating heartbeat for {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<bool> UpdateBootInfoAsync(string chargePointId, string vendor, string model, 
            string serialNumber, string firmwareVersion, int heartbeatInterval)
        {
            try
            {
                var chargePoint = await GetByIdAsync(chargePointId);
                if (chargePoint == null)
                    return false;
                    
                chargePoint.Vendor = vendor;
                chargePoint.Model = model;
                chargePoint.SerialNumber = serialNumber;
                chargePoint.FirmwareVersion = firmwareVersion;
                chargePoint.HeartbeatInterval = heartbeatInterval;
                chargePoint.LastBootTime = DateTime.UtcNow;
                chargePoint.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated boot info for {ChargePointId}", chargePointId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating boot info for {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<ConnectorEntity?> GetConnectorAsync(string chargePointId, int connectorId)
        {
            return await _context.Connectors
                .Include(c => c.ChargePoint)
                .FirstOrDefaultAsync(c => c.ChargePoint.ChargePointId == chargePointId && 
                                          c.ConnectorId == connectorId);
        }
        
        public async Task<ConnectorEntity> UpdateConnectorStatusAsync(string chargePointId, int connectorId, 
            string status, string? errorCode = null, string? info = null)
        {
            var connector = await GetConnectorAsync(chargePointId, connectorId);
            
            if (connector == null)
            {
                // Создаем новый коннектор если не существует
                var chargePoint = await GetByIdAsync(chargePointId);
                if (chargePoint == null)
                    throw new ArgumentException($"Charge point {chargePointId} not found");
                    
                connector = new ConnectorEntity()
                {
                    ConnectorId = connectorId,
                    ChargePointId = chargePoint.Id,
                    Status = status,
                    ErrorCode = errorCode,
                    Info = info,
                    StatusTimestamp = DateTime.UtcNow
                };
                
                _context.Connectors.Add(connector);
            }
            else
            {
                connector.UpdateStatus(status, errorCode, info);
                _context.Connectors.Update(connector);
            }
            
            await _context.SaveChangesAsync();
            return connector;
        }
        
        public async Task<IEnumerable<ConnectorEntity>> GetConnectorsByStatusAsync(string chargePointId, string status)
        {
            return await _context.Connectors
                .Where(c => c.ChargePoint.ChargePointId == chargePointId && c.Status == status)
                .ToListAsync();
        }
        
        public async Task<int> GetCountAsync()
        {
            return await _context.ChargePoints.CountAsync();
        }
        
        public async Task<int> GetOnlineCountAsync()
        {
            return await _context.ChargePoints
                .CountAsync(cp => cp.Status == "Online");
        }
        
        public async Task<Dictionary<string, int>> GetStatusDistributionAsync()
        {
            return await _context.ChargePoints
                .GroupBy(cp => cp.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }
        
        public async Task<IEnumerable<ChargePointEntity>> GetChargePointsByIdsAsync(IEnumerable<string> chargePointIds)
        {
            return await _context.ChargePoints
                .Where(cp => chargePointIds.Contains(cp.ChargePointId))
                .Include(cp => cp.Connectors)
                .ToListAsync();
        }
        
        public async Task<bool> BulkUpdateStatusAsync(IEnumerable<string> chargePointIds, string status)
        {
            try
            {
                var chargePoints = await GetChargePointsByIdsAsync(chargePointIds);
                
                foreach (var cp in chargePoints)
                {
                    cp.UpdateStatus(status);
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating status");
                return false;
            }
        }
    }