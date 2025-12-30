using Domain.Entities.Station;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    // Basic CRUD
        Task<TransactionEntity?> GetByIdAsync(int id);
        Task<TransactionEntity?> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<TransactionEntity>> GetAllAsync();
        Task<TransactionEntity> CreateAsync(TransactionEntity transaction);
        Task<TransactionEntity> UpdateAsync(TransactionEntity transaction);
        Task<bool> DeleteAsync(int id);
        
        // Query operations
        Task<IEnumerable<TransactionEntity>> GetByChargePointIdAsync(string chargePointId);
        Task<IEnumerable<TransactionEntity>> GetByChargePointAndConnectorAsync(string chargePointId, int connectorId);
        Task<IEnumerable<TransactionEntity>> GetByIdTagAsync(string idTag);
        Task<IEnumerable<TransactionEntity>> GetActiveTransactionsAsync();
        Task<TransactionEntity?> GetActiveTransactionAsync(string chargePointId, int connectorId);
        Task<IEnumerable<TransactionEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<TransactionEntity>> GetByStatusAsync(TransactionStatusEnum status);
        
        // Transaction operations
        Task<TransactionEntity?> StartTransactionAsync(string chargePointId, int connectorId, 
            string idTag, double meterStart, string transactionId);
        Task<TransactionEntity?> StopTransactionAsync(string transactionId, double meterStop, 
            string? reason = null, DateTime? stopTimestamp = null);
        Task<bool> UpdateTransactionMeterValueAsync(string transactionId, double meterValue);
        Task<bool> AddMeterValueAsync(string transactionId, MeterValueEntity meterValue);
        
        // Statistics
        Task<int> GetCountAsync();
        Task<int> GetActiveCountAsync();
        Task<double> GetTotalEnergyConsumedAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<double> GetRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, int>> GetTransactionsPerDayAsync(DateTime startDate, DateTime endDate);
        
        // Complex queries
        Task<IEnumerable<TransactionEntity>> GetTransactionsWithMeterValuesAsync(string transactionId);
        Task<IEnumerable<TransactionEntity>> GetTransactionsByChargePointWithDetailsAsync(string chargePointId);
}