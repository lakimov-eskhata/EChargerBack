namespace Application.Interfaces.Services;

public interface IOCPPService
{
    // Charge Point operations
    Task<bool> ProcessBootNotificationAsync(string chargePointId, BootNotificationData data);
    Task<bool> ProcessHeartbeatAsync(string chargePointId);
    Task<bool> UpdateConnectorStatusAsync(string chargePointId, int connectorId, 
        string status, string? errorCode = null, string? info = null);
        
    // Transaction operations
    Task<TransactionResult> StartTransactionAsync(string chargePointId, int connectorId, 
        string idTag, double meterStart);
    Task<TransactionResult> StopTransactionAsync(string transactionId, double meterStop, 
        string? reason = null);
    Task<bool> UpdateTransactionMeterValueAsync(string transactionId, double meterValue);
        
    // Remote commands
    Task<bool> RemoteStartTransactionAsync(string chargePointId, int connectorId, 
        string idTag, int? chargingProfileId = null);
    Task<bool> RemoteStopTransactionAsync(string transactionId);
    Task<bool> ResetChargePointAsync(string chargePointId, string resetType);
    Task<bool> UnlockConnectorAsync(string chargePointId, int connectorId);
        
    // Status and monitoring
    Task<ChargePointStatus> GetChargePointStatusAsync(string chargePointId);
    Task<IEnumerable<ActiveTransactionInfo>> GetActiveTransactionsAsync();
    Task<OCPPSystemStatus> GetSystemStatusAsync();
        
    // Data transfer
    Task<DataTransferResult> ProcessDataTransferAsync(string chargePointId, 
        string vendorId, string messageId, string? data = null);
}

public class BootNotificationData
    {
        public string ChargePointVendor { get; set; } = string.Empty;
        public string ChargePointModel { get; set; } = string.Empty;
        public string ChargePointSerialNumber { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string? Iccid { get; set; }
        public string? Imsi { get; set; }
        public string? MeterType { get; set; }
        public string? MeterSerialNumber { get; set; }
        public int HeartbeatInterval { get; set; } = 300;
    }
    
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string? IdTagInfo { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class ChargePointStatus
    {
        public string ChargePointId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastHeartbeat { get; set; }
        public DateTime? LastBootTime { get; set; }
        public IEnumerable<ConnectorStatus> Connectors { get; set; } = new List<ConnectorStatus>();
    }
    
    public class ConnectorStatus
    {
        public int ConnectorId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public string? Info { get; set; }
        public DateTime? StatusTimestamp { get; set; }
        public string? ActiveTransactionId { get; set; }
        public double? MeterValue { get; set; }
    }
    
    public class ActiveTransactionInfo
    {
        public string TransactionId { get; set; } = string.Empty;
        public string ChargePointId { get; set; } = string.Empty;
        public int ConnectorId { get; set; }
        public string IdTag { get; set; } = string.Empty;
        public DateTime StartTimestamp { get; set; }
        public double MeterStart { get; set; }
        public double? CurrentMeterValue { get; set; }
        public TimeSpan Duration { get; set; }
        public double EnergyConsumed { get; set; }
    }
    
    public class OCPPSystemStatus
    {
        public int TotalChargePoints { get; set; }
        public int OnlineChargePoints { get; set; }
        public int OfflineChargePoints { get; set; }
        public int ActiveTransactions { get; set; }
        public double TotalEnergyConsumedToday { get; set; }
        public Dictionary<string, int> ChargePointsByProtocol { get; set; } = new();
        public Dictionary<string, int> ConnectorsByStatus { get; set; } = new();
    }
    
    public class DataTransferResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty; // Accepted, Rejected, UnknownMessageId, UnknownVendorId
        public string? Data { get; set; }
    }