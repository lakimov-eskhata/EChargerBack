namespace Domain.Entities.Station
{
    public class ChargePointEntity
    {
        public int Id { get; set; }
        public string ChargePointId { get; set; } = null!; // Уникальный идентификатор станции
        public string Name { get; set; } = string.Empty;
        public string ProtocolVersion { get; set; } = "1.6"; // ocpp1.6, ocpp2.0, ocpp2.1
        public string Vendor { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline"; // Online, Offline, Faulted
        public int? ConnectorCount { get; set; }
        public int? HeartbeatInterval { get; set; }
        public string MeterType { get; set; } = string.Empty;
        public string MeterSerialNumber { get; set; } = string.Empty;
        public string? Iccid { get; set; } // Для GSM станций
        public string? Imsi { get; set; } // Для GSM станций
        public DateTime? LastBootTime { get; set; }
        public DateTime? LastHeartbeat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Навигационные свойства
        public virtual ICollection<ConnectorEntity> Connectors { get; set; } = new List<ConnectorEntity>();
        public virtual ICollection<TransactionEntity> Transactions { get; set; } = new List<TransactionEntity>();


        // Методы
        public void UpdateStatus(string status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}