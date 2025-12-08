
namespace Domain.Entities.Station
{
    public class ChargePointEntity
    {
        public ChargePointEntity()
        {
            Transactions = new HashSet<TransactionEntity>();
        }

        public string ChargePointId { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientCertThumb { get; set; }

        public virtual ICollection<TransactionEntity> Transactions { get; set; }
    }
}
