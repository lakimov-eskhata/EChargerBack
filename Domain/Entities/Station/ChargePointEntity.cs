using Domain.Entities.User;

namespace Domain.Entities.Station
{
    public class ChargePointEntity
    {
        public int Id { get; set; }
        public string ChargePointId { get; set; } = null!; // Уникальный идентификатор для OCPP подключения
        public string? Name { get; set; } // Название (необязательно)
        public string ProtocolVersion { get; set; } = "1.6"; // ocpp1.6, ocpp2.0, ocpp2.1
        public string? Vendor { get; set; } // Производитель (необязательно)
        public string? Model { get; set; } // Модель (необязательно)
        public string? SerialNumber { get; set; } // Серийный номер (необязательно)
        public string? FirmwareVersion { get; set; } // Версия прошивки (необязательно)
        public string Status { get; set; } = "Offline"; // Online, Offline, Faulted
        public int? ConnectorCount { get; set; } // Количество коннекторов (автоматически рассчитывается)
        public int? HeartbeatInterval { get; set; } // Интервал heartbeat в секундах
        public string? MeterType { get; set; } // Тип счетчика (необязательно)
        public string? MeterSerialNumber { get; set; } // Серийный номер счетчика (необязательно)
        public string? Iccid { get; set; } // Для GSM станций
        public string? Imsi { get; set; } // Для GSM станций
        public DateTime? LastBootTime { get; set; }
        public DateTime? LastHeartbeat { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Внешний ключ для станции
        public int? StationId { get; set; }
        public virtual StationEntity? Station { get; set; }

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