namespace Domain.Entities.Station;

public class ConnectorEntity
{
    public int Id { get; set; }
    public int ConnectorId { get; set; } // Номер коннектора на станции (1, 2, 3...)
    public string Status { get; set; } = "Available"; // Available, Preparing, Charging, SuspendedEVSE, SuspendedEV, Finishing, Reserved, Unavailable, Faulted
    public string? ErrorCode { get; set; }
    public string? Info { get; set; }
    public DateTime? StatusTimestamp { get; set; }
    public int? TransactionId { get; set; }
    public double? MeterValue { get; set; } // Текущее значение счетчика
        
    public int ChargePointId { get; set; }
    public virtual ChargePointEntity ChargePoint { get; set; } = null!;
    public virtual TransactionEntity? ActiveTransaction { get; set; }
        
    // Методы
    public void UpdateStatus(string status, string? errorCode = null, string? info = null)
    {
        Status = status;
        ErrorCode = errorCode;
        Info = info;
        StatusTimestamp = DateTime.UtcNow;
    }
}