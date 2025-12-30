using System.Transactions;
using Domain.Enums;

namespace Domain.Entities.Station
{
    public class TransactionEntity
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } = null!; // Уникальный ID транзакции
        public string IdTag { get; set; } = null!; // RFID метка пользователя
        public int ConnectorId { get; set; }
        public DateTime StartTimestamp { get; set; }
        public DateTime? StopTimestamp { get; set; }
        public DateTime? StopValueTimestamp { get; set; }
        public double? MeterStart { get; set; }
        public double? MeterStop { get; set; }
        public double? MeterValue { get; set; } // Текущее значение
        public string? Reason { get; set; } // Причина остановки
        public int Status { get; set; }
        public string? ParentIdTag { get; set; }

        public int ChargePointId { get; set; }
        public virtual ChargePointEntity ChargePoint { get; set; } = null!;

        // Навигационные свойства
        public virtual ICollection<MeterValueEntity> MeterValues { get; set; } = new List<MeterValueEntity>();

        // Методы
        public void Start(double meterStart)
        {
            MeterStart = meterStart;
            StartTimestamp = DateTime.UtcNow;
            Status = TransactionStatusEnum.Started.Value;
        }

        public void UpdateMeterValue(double meterValue)
        {
            MeterValue = meterValue;
        }

        public void Stop(double meterStop, string? reason = null)
        {
            MeterStop = meterStop;
            StopTimestamp = DateTime.UtcNow;
            StopValueTimestamp = DateTime.UtcNow;
            Reason = reason;
            Status = TransactionStatusEnum.Completed.Value;
        }

        public TimeSpan GetDuration()
        {
            var endTime = StopTimestamp ?? DateTime.UtcNow;
            return endTime - StartTimestamp;
        }

        public double GetEnergyConsumed()
        {
            if (MeterStart.HasValue && MeterStop.HasValue)
                return MeterStop.Value - MeterStart.Value;

            if (MeterStart.HasValue && MeterValue.HasValue)
                return MeterValue.Value - MeterStart.Value;

            return 0;
        }
    }
}