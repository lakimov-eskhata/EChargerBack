using Domain.Entities.Station;

namespace Application.Interfaces.Repositories;

public interface IChargePointRepository
{
    // Basic CRUD
    Task<ChargePointEntity?> GetByIdAsync(string chargePointId);
    Task<IEnumerable<ChargePointEntity>> GetAllAsync();
    Task<ChargePointEntity> CreateAsync(ChargePointEntity chargePoint);
    Task<ChargePointEntity> UpdateAsync(ChargePointEntity chargePoint);
    Task<bool> DeleteAsync(string chargePointId);
        
    // Specific queries
    Task<ChargePointEntity?> GetBySerialNumberAsync(string serialNumber);
    Task<IEnumerable<ChargePointEntity>> GetByStatusAsync(string status);
    Task<IEnumerable<ChargePointEntity>> GetByProtocolVersionAsync(string protocolVersion);
    Task<ChargePointEntity?> GetOrCreateAsync(string chargePointId, Action<ChargePointEntity>? initialize = null);
        
    // Status operations
    Task<bool> UpdateStatusAsync(string chargePointId, string status);
    Task<bool> UpdateHeartbeatAsync(string chargePointId);
    Task<bool> UpdateBootInfoAsync(string chargePointId, string vendor, string model, 
        string serialNumber, string firmwareVersion, int heartbeatInterval);
        
    // Connector operations
    Task<ConnectorEntity?> GetConnectorAsync(string chargePointId, int connectorId);
    Task<ConnectorEntity> UpdateConnectorStatusAsync(string chargePointId, int connectorId, 
        string status, string? errorCode = null, string? info = null);
    Task<IEnumerable<ConnectorEntity>> GetConnectorsByStatusAsync(string chargePointId, string status);
        
    // Count and statistics
    Task<int> GetCountAsync();
    Task<int> GetOnlineCountAsync();
    Task<Dictionary<string, int>> GetStatusDistributionAsync();
        
    // Batch operations
    Task<IEnumerable<ChargePointEntity>> GetChargePointsByIdsAsync(IEnumerable<string> chargePointIds);
    Task<bool> BulkUpdateStatusAsync(IEnumerable<string> chargePointIds, string status);
}