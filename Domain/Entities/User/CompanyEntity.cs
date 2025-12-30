using Domain.Entities.Station;

namespace Domain.Entities.User
{
    public class CompanyEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public decimal Balance { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public virtual ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
        public virtual ICollection<ChargePointEntity> ChargePoints { get; set; } = new List<ChargePointEntity>();

        // Методы
        public void UpdateBalance(decimal amount)
        {
            Balance += amount;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateInfo(string name, string? description, string? contactEmail, string? contactPhone, string? address)
        {
            Name = name;
            Description = description;
            ContactEmail = contactEmail;
            ContactPhone = contactPhone;
            Address = address;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
