using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class OCPPService : IOCPPService
    {
        private readonly IChargePointRepository _chargePointRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IChargePointConnectionStorage _connectionStorage;
        private readonly ILogger<OCPPService> _logger;
        
        public OCPPService(
            IChargePointRepository chargePointRepository,
            ITransactionRepository transactionRepository,
            IChargePointConnectionStorage connectionStorage,
            ILogger<OCPPService> logger)
        {
            _chargePointRepository = chargePointRepository;
            _transactionRepository = transactionRepository;
            _connectionStorage = connectionStorage;
            _logger = logger;
        }
        
        public async Task<bool> ProcessBootNotificationAsync(string chargePointId, BootNotificationData data)
        {
            try
            {
                _logger.LogInformation("Processing BootNotification for {ChargePointId}", chargePointId);
                
                var chargePoint = await _chargePointRepository.GetOrCreateAsync(chargePointId, cp =>
                {
                    cp.Vendor = data.ChargePointVendor;
                    cp.Model = data.ChargePointModel;
                    cp.SerialNumber = data.ChargePointSerialNumber;
                    cp.FirmwareVersion = data.FirmwareVersion;
                    cp.Iccid = data.Iccid;
                    cp.Imsi = data.Imsi;
                    cp.MeterType = data.MeterType;
                    cp.MeterSerialNumber = data.MeterSerialNumber;
                    cp.HeartbeatInterval = data.HeartbeatInterval;
                    cp.LastBootTime = DateTime.UtcNow;
                    cp.Status = "Online";
                });
                
                if (chargePoint == null)
                {
                    _logger.LogError("Failed to process BootNotification for {ChargePointId}", chargePointId);
                    return false;
                }
                
                // Если у станции есть коннекторы, создаем их по умолчанию
                if (chargePoint.ConnectorCount.HasValue && chargePoint.ConnectorCount.Value > 0)
                {
                    for (int i = 1; i <= chargePoint.ConnectorCount.Value; i++)
                    {
                        await _chargePointRepository.UpdateConnectorStatusAsync(
                            chargePointId, i, "Available");
                    }
                }
                
                _logger.LogInformation("BootNotification processed successfully for {ChargePointId}", chargePointId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing BootNotification for {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<bool> ProcessHeartbeatAsync(string chargePointId)
        {
            try
            {
                var success = await _chargePointRepository.UpdateHeartbeatAsync(chargePointId);
                if (success)
                {
                    _logger.LogDebug("Heartbeat updated for {ChargePointId}", chargePointId);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing heartbeat for {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<bool> UpdateConnectorStatusAsync(string chargePointId, int connectorId, 
            string status, string? errorCode = null, string? info = null)
        {
            try
            {
                var connector = await _chargePointRepository.UpdateConnectorStatusAsync(
                    chargePointId, connectorId, status, errorCode, info);
                    
                _logger.LogInformation(
                    "Updated connector {ConnectorId} status to {Status} on {ChargePointId}",
                    connectorId, status, chargePointId);
                    
                return connector != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connector status for {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<TransactionResult> StartTransactionAsync(string chargePointId, int connectorId, 
            string idTag, double meterStart)
        {
            try
            {
                // Генерируем уникальный ID транзакции
                var transactionId = GenerateTransactionId(chargePointId, connectorId);
                
                var transaction = await _transactionRepository.StartTransactionAsync(
                    chargePointId, connectorId, idTag, meterStart, transactionId);
                
                if (transaction == null)
                {
                    return new TransactionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to start transaction"
                    };
                }
                
                return new TransactionResult
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    IdTagInfo = "Accepted" // В реальной системе здесь проверка авторизации
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting transaction on {ChargePointId}", chargePointId);
                return new TransactionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        public async Task<TransactionResult> StopTransactionAsync(string transactionId, double meterStop, 
            string? reason = null)
        {
            try
            {
                var transaction = await _transactionRepository.StopTransactionAsync(
                    transactionId, meterStop, reason);
                
                if (transaction == null)
                {
                    return new TransactionResult
                    {
                        Success = false,
                        ErrorMessage = $"Transaction {transactionId} not found"
                    };
                }
                
                return new TransactionResult
                {
                    Success = true,
                    TransactionId = transaction.TransactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping transaction {TransactionId}", transactionId);
                return new TransactionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        public async Task<bool> UpdateTransactionMeterValueAsync(string transactionId, double meterValue)
        {
            try
            {
                var success = await _transactionRepository.UpdateTransactionMeterValueAsync(
                    transactionId, meterValue);
                    
                if (success)
                {
                    _logger.LogDebug("Updated meter value for transaction {TransactionId}", transactionId);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating meter value for transaction {TransactionId}", transactionId);
                return false;
            }
        }
        
        public async Task<bool> RemoteStartTransactionAsync(string chargePointId, int connectorId, 
            string idTag, int? chargingProfileId = null)
        {
            try
            {
                // Проверяем подключение станции
                var isConnected = await _connectionStorage.IsConnectedAsync(chargePointId);
                if (!isConnected)
                {
                    _logger.LogWarning("Cannot send remote start: {ChargePointId} is not connected", chargePointId);
                    return false;
                }
                
                // Отправляем команду через WebSocket
                var message = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "RemoteStartTransaction",
                    @params = new
                    {
                        connectorId,
                        idTag,
                        chargingProfile = chargingProfileId.HasValue ? new
                        {
                            chargingProfileId = chargingProfileId.Value
                        } : null
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(message);
                await _connectionStorage.SendMessageAsync(chargePointId, json);
                
                _logger.LogInformation(
                    "Sent RemoteStartTransaction to {ChargePointId} for connector {ConnectorId}",
                    chargePointId, connectorId);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending RemoteStartTransaction to {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<bool> RemoteStopTransactionAsync(string transactionId)
        {
            try
            {
                // Находим транзакцию
                var transaction = await _transactionRepository.GetByTransactionIdAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                    return false;
                }
                
                var chargePointId = transaction.ChargePoint.ChargePointId;
                
                // Проверяем подключение станции
                var isConnected = await _connectionStorage.IsConnectedAsync(chargePointId);
                if (!isConnected)
                {
                    _logger.LogWarning("Cannot send remote stop: {ChargePointId} is not connected", chargePointId);
                    return false;
                }
                
                // Отправляем команду через WebSocket
                var message = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "RemoteStopTransaction",
                    @params = new
                    {
                        transactionId
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(message);
                await _connectionStorage.SendMessageAsync(chargePointId, json);
                
                _logger.LogInformation(
                    "Sent RemoteStopTransaction to {ChargePointId} for transaction {TransactionId}",
                    chargePointId, transactionId);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending RemoteStopTransaction for {TransactionId}", transactionId);
                return false;
            }
        }
        
        public async Task<bool> ResetChargePointAsync(string chargePointId, string resetType)
        {
            try
            {
                // Проверяем подключение станции
                var isConnected = await _connectionStorage.IsConnectedAsync(chargePointId);
                if (!isConnected)
                {
                    _logger.LogWarning("Cannot send reset: {ChargePointId} is not connected", chargePointId);
                    return false;
                }
                
                // Отправляем команду через WebSocket
                var message = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "Reset",
                    @params = new
                    {
                        type = resetType // Hard, Soft
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(message);
                await _connectionStorage.SendMessageAsync(chargePointId, json);
                
                _logger.LogInformation("Sent Reset command to {ChargePointId} with type {ResetType}", 
                    chargePointId, resetType);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Reset to {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<bool> UnlockConnectorAsync(string chargePointId, int connectorId)
        {
            try
            {
                // Проверяем подключение станции
                var isConnected = await _connectionStorage.IsConnectedAsync(chargePointId);
                if (!isConnected)
                {
                    _logger.LogWarning("Cannot send unlock: {ChargePointId} is not connected", chargePointId);
                    return false;
                }
                
                // Отправляем команду через WebSocket
                var message = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "UnlockConnector",
                    @params = new
                    {
                        connectorId
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(message);
                await _connectionStorage.SendMessageAsync(chargePointId, json);
                
                _logger.LogInformation("Sent UnlockConnector to {ChargePointId} for connector {ConnectorId}", 
                    chargePointId, connectorId);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending UnlockConnector to {ChargePointId}", chargePointId);
                return false;
            }
        }
        
        public async Task<ChargePointStatus> GetChargePointStatusAsync(string chargePointId)
        {
            try
            {
                var chargePoint = await _chargePointRepository.GetByIdAsync(chargePointId);
                if (chargePoint == null)
                {
                    return new ChargePointStatus
                    {
                        ChargePointId = chargePointId,
                        Status = "NotFound"
                    };
                }
                
                var connectors = new List<ConnectorStatus>();
                foreach (var connector in chargePoint.Connectors)
                {
                    var activeTransaction = await _transactionRepository.GetActiveTransactionAsync(
                        chargePointId, connector.ConnectorId);
                    
                    connectors.Add(new ConnectorStatus
                    {
                        ConnectorId = connector.ConnectorId,
                        Status = connector.Status,
                        ErrorCode = connector.ErrorCode,
                        Info = connector.Info,
                        StatusTimestamp = connector.StatusTimestamp,
                        ActiveTransactionId = activeTransaction?.TransactionId,
                        MeterValue = connector.MeterValue
                    });
                }
                
                return new ChargePointStatus
                {
                    ChargePointId = chargePoint.ChargePointId,
                    Status = chargePoint.Status,
                    LastHeartbeat = chargePoint.LastHeartbeat,
                    LastBootTime = chargePoint.LastBootTime,
                    Connectors = connectors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for {ChargePointId}", chargePointId);
                return new ChargePointStatus
                {
                    ChargePointId = chargePointId,
                    Status = "Error"
                };
            }
        }
        
        public async Task<IEnumerable<ActiveTransactionInfo>> GetActiveTransactionsAsync()
        {
            try
            {
                var transactions = await _transactionRepository.GetActiveTransactionsAsync();
                
                return transactions.Select(t => new ActiveTransactionInfo
                {
                    TransactionId = t.TransactionId,
                    ChargePointId = t.ChargePoint.ChargePointId,
                    ConnectorId = t.ConnectorId,
                    IdTag = t.IdTag,
                    StartTimestamp = t.StartTimestamp,
                    MeterStart = t.MeterStart ?? 0,
                    CurrentMeterValue = t.MeterValue,
                    Duration = t.GetDuration(),
                    EnergyConsumed = t.GetEnergyConsumed()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active transactions");
                return new List<ActiveTransactionInfo>();
            }
        }
        
        public async Task<OCPPSystemStatus> GetSystemStatusAsync()
        {
            try
            {
                var totalChargePoints = await _chargePointRepository.GetCountAsync();
                var onlineChargePoints = await _chargePointRepository.GetOnlineCountAsync();
                var activeTransactions = await _transactionRepository.GetActiveCountAsync();
                
                // Получаем распределение по протоколам
                var protocols = new[] { "1.6", "2.0", "2.1" };
                var chargePointsByProtocol = new Dictionary<string, int>();
                
                foreach (var protocol in protocols)
                {
                    var count = (await _chargePointRepository.GetByProtocolVersionAsync(protocol)).Count();
                    chargePointsByProtocol[protocol] = count;
                }
                
                // Получаем распределение коннекторов по статусам
                var allChargePoints = await _chargePointRepository.GetAllAsync();
                var connectorsByStatus = allChargePoints
                    .SelectMany(cp => cp.Connectors)
                    .GroupBy(c => c.Status)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                return new OCPPSystemStatus
                {
                    TotalChargePoints = totalChargePoints,
                    OnlineChargePoints = onlineChargePoints,
                    OfflineChargePoints = totalChargePoints - onlineChargePoints,
                    ActiveTransactions = activeTransactions,
                    TotalEnergyConsumedToday = await _transactionRepository.GetTotalEnergyConsumedAsync(
                        DateTime.UtcNow.Date, DateTime.UtcNow),
                    ChargePointsByProtocol = chargePointsByProtocol,
                    ConnectorsByStatus = connectorsByStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                return new OCPPSystemStatus();
            }
        }
        
        public async Task<DataTransferResult> ProcessDataTransferAsync(string chargePointId, 
            string vendorId, string messageId, string? data = null)
        {
            try
            {
                _logger.LogInformation(
                    "Processing DataTransfer from {ChargePointId}, Vendor: {VendorId}, Message: {MessageId}",
                    chargePointId, vendorId, messageId);
                
                // Здесь можно добавить логику обработки vendor-specific сообщений
                // Например, логирование, передача в другую систему и т.д.
                
                return new DataTransferResult
                {
                    Success = true,
                    Status = "Accepted",
                    Data = "Processed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DataTransfer from {ChargePointId}", chargePointId);
                return new DataTransferResult
                {
                    Success = false,
                    Status = "Rejected",
                    Data = ex.Message
                };
            }
        }
        
        private string GenerateTransactionId(string chargePointId, int connectorId)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"{chargePointId}-{connectorId}-{timestamp}-{random}";
        }
    }