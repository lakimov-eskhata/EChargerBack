using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.Station;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class TransactionRepository(
    IApplicationDbContext context,
    ILogger<TransactionRepository> logger,
    IChargePointRepository chargePointRepository) : ITransactionRepository
{
    public async Task<TransactionEntity?> GetByIdAsync(int id)
    {
        return await context.Transactions
            .Include(t => t.ChargePoint)
            .Include(t => t.MeterValues)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TransactionEntity?> GetByTransactionIdAsync(string transactionId)
    {
        return await context.Transactions
            .Include(t => t.ChargePoint)
            .Include(t => t.MeterValues)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
    }

    public async Task<IEnumerable<TransactionEntity>> GetAllAsync()
    {
        return await context.Transactions
            .Include(t => t.ChargePoint)
            .OrderByDescending(t => t.StartTimestamp)
            .ToListAsync();
    }

    public async Task<TransactionEntity> CreateAsync(TransactionEntity transaction)
    {
        try
        {
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            logger.LogInformation("Created transaction: {TransactionId}", transaction.TransactionId);
            return transaction;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating transaction: {TransactionId}", transaction.TransactionId);
            throw;
        }
    }

    public async Task<TransactionEntity> UpdateAsync(TransactionEntity transaction)
    {
        try
        {
            context.Transactions.Update(transaction);
            await context.SaveChangesAsync();

            return transaction;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating transaction: {TransactionId}", transaction.TransactionId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var transaction = await GetByIdAsync(id);
            if (transaction == null)
                return false;

            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting transaction ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<TransactionEntity>> GetByChargePointIdAsync(string chargePointId)
    {
        return await context.Transactions
            .Where(t => t.ChargePoint.ChargePointId == chargePointId)
            .OrderByDescending(t => t.StartTimestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<TransactionEntity>> GetByChargePointAndConnectorAsync(string chargePointId, int connectorId)
    {
        return await context.Transactions
            .Where(t => t.ChargePoint.ChargePointId == chargePointId && t.ConnectorId == connectorId)
            .OrderByDescending(t => t.StartTimestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<TransactionEntity>> GetByIdTagAsync(string idTag)
    {
        return await context.Transactions
            .Where(t => t.IdTag == idTag)
            .OrderByDescending(t => t.StartTimestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<TransactionEntity>> GetActiveTransactionsAsync()
    {
        var activeStatuses = new[] { TransactionStatusEnum.Started.Value, TransactionStatusEnum.InProgress.Value };

        return await context.Transactions
            .Where(t => activeStatuses.Contains(t.Status))
            .Include(t => t.ChargePoint)
            .OrderBy(t => t.StartTimestamp)
            .ToListAsync();
    }

    public async Task<TransactionEntity?> GetActiveTransactionAsync(string chargePointId, int connectorId)
    {
        var activeStatuses = new[] { TransactionStatusEnum.Started.Value, TransactionStatusEnum.InProgress.Value };

        return await context.Transactions
            .Include(t => t.ChargePoint)
            .FirstOrDefaultAsync(t => t.ChargePoint.ChargePointId == chargePointId &&
                                      t.ConnectorId == connectorId &&
                                      activeStatuses.Contains(t.Status));
    }

    public async Task<IEnumerable<TransactionEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await context.Transactions
            .Where(t => t.StartTimestamp >= startDate && t.StartTimestamp <= endDate)
            .Include(t => t.ChargePoint)
            .OrderByDescending(t => t.StartTimestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<TransactionEntity>> GetByStatusAsync(TransactionStatusEnum status)
    {
        return await context.Transactions
            .Where(t => t.Status == status.Value)
            .Include(t => t.ChargePoint)
            .OrderByDescending(t => t.StartTimestamp)
            .ToListAsync();
    }

    public async Task<TransactionEntity?> StartTransactionAsync(string chargePointId, int connectorId,
        string idTag, double meterStart, string transactionId)
    {
        try
        {
            var chargePoint = await context.ChargePoints
                .FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointId);

            if (chargePoint == null)
            {
                logger.LogWarning("Charge point {ChargePointId} not found", chargePointId);
                return null;
            }

            // Проверяем, есть ли активная транзакция на этом коннекторе
            var activeTransaction = await GetActiveTransactionAsync(chargePointId, connectorId);
            if (activeTransaction != null)
            {
                logger.LogWarning("Active transaction already exists on {ChargePointId} connector {ConnectorId}",
                    chargePointId, connectorId);
                return null;
            }

            var transaction = new TransactionEntity()
            {
                TransactionId = transactionId,
                IdTag = idTag,
                ConnectorId = connectorId,
                ChargePointId = chargePoint.Id,
                MeterStart = meterStart,
                StartTimestamp = DateTime.UtcNow,
                Status = TransactionStatusEnum.Started.Value
            };

            await CreateAsync(transaction);

            // Обновляем статус коннектора
            await chargePointRepository.UpdateConnectorStatusAsync(chargePointId, connectorId, "Charging");

            logger.LogInformation("Started transaction {TransactionId} on {ChargePointId} connector {ConnectorId}",
                transactionId, chargePointId, connectorId);

            return transaction;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting transaction on {ChargePointId}", chargePointId);
            return null;
        }
    }

    public async Task<TransactionEntity?> StopTransactionAsync(string transactionId, double meterStop,
        string? reason = null, DateTime? stopTimestamp = null)
    {
        try
        {
            var transaction = await GetByTransactionIdAsync(transactionId);
            if (transaction == null)
            {
                logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                return null;
            }

            transaction.Stop(meterStop, reason);

            if (stopTimestamp.HasValue)
            {
                transaction.StopTimestamp = stopTimestamp.Value;
            }

            await UpdateAsync(transaction);

            // Обновляем статус коннектора
            await chargePointRepository.UpdateConnectorStatusAsync(
                transaction.ChargePoint.ChargePointId,
                transaction.ConnectorId,
                "Available");

            logger.LogInformation("Stopped transaction {TransactionId}", transactionId);
            return transaction;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping transaction {TransactionId}", transactionId);
            return null;
        }
    }

    public async Task<bool> UpdateTransactionMeterValueAsync(string transactionId, double meterValue)
    {
        try
        {
            var transaction = await GetByTransactionIdAsync(transactionId);
            if (transaction == null)
                return false;

            transaction.UpdateMeterValue(meterValue);
            transaction.Status = TransactionStatusEnum.InProgress.Value;

            await UpdateAsync(transaction);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating meter value for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> AddMeterValueAsync(string transactionId, MeterValueEntity meterValue)
    {
        try
        {
            var transaction = await GetByTransactionIdAsync(transactionId);
            if (transaction == null)
                return false;

            meterValue.TransactionId = transaction.Id;
            context.MeterValues.Add(meterValue);

            // Обновляем текущее значение в транзакции
            transaction.MeterValue = meterValue.Value;

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding meter value for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<int> GetCountAsync()
    {
        return await context.Transactions.CountAsync();
    }

    public async Task<int> GetActiveCountAsync()
    {
        var activeStatuses = new[] { TransactionStatusEnum.Started.Value, TransactionStatusEnum.InProgress.Value };
        return await context.Transactions
            .CountAsync(t => activeStatuses.Contains(t.Status));
    }

    public async Task<double> GetTotalEnergyConsumedAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = context.Transactions.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(t => t.StartTimestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.StartTimestamp <= endDate.Value);

        var transactions = await query.ToListAsync();
        return transactions.Sum(t => t.GetEnergyConsumed());
    }

    public async Task<double> GetRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Здесь можно добавить логику расчета стоимости
        // Например: энергия * тариф
        var totalEnergy = await GetTotalEnergyConsumedAsync(startDate, endDate);
        const double pricePerKwh = 0.15; // Примерная цена
        return totalEnergy * pricePerKwh / 1000; // Переводим в кВтч
    }

    public async Task<Dictionary<string, int>> GetTransactionsPerDayAsync(DateTime startDate, DateTime endDate)
    {
        return await context.Transactions
            .Where(t => t.StartTimestamp >= startDate && t.StartTimestamp <= endDate)
            .GroupBy(t => t.StartTimestamp.Date)
            .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count);
    }

    public async Task<IEnumerable<TransactionEntity>> GetTransactionsWithMeterValuesAsync(string transactionId)
    {
        return await context.Transactions
            .Where(t => t.TransactionId == transactionId)
            .Include(t => t.MeterValues.OrderBy(mv => mv.Timestamp))
            .ToListAsync();
    }

    public async Task<IEnumerable<TransactionEntity>> GetTransactionsByChargePointWithDetailsAsync(string chargePointId)
    {
        return await context.Transactions
            .Where(t => t.ChargePoint.ChargePointId == chargePointId)
            .Include(t => t.MeterValues)
            .Include(t => t.ChargePoint)
            .OrderByDescending(t => t.StartTimestamp)
            .ToListAsync();
    }
}