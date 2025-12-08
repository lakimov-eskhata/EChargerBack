
namespace Domain.Entities.Station
{
    public partial class ConnectorStatusEntity
    {
        public string ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public string ConnectorName { get; set; }
        public string LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }
        public double? LastMeter { get; set; }
        public DateTime? LastMeterTime { get; set; }
        public virtual ChargePointEntity ChargePoint { get; set; }

        public override string ToString()
        {
            string chargePointName = ChargePoint?.Name ?? ChargePointId;
            if (!string.IsNullOrEmpty(ConnectorName))
            {
                return ConnectorName;
            }

            return $"{chargePointName}:{ConnectorId}";
        }
    }
}

