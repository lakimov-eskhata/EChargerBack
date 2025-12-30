using Domain.Entities.User;

namespace Domain.Entities.Station
{
    public class StationEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; } // Координаты для карт
        public decimal? Longitude { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Внешний ключ для компании-владельца
        public int? CompanyId { get; set; }
        public virtual CompanyEntity? Company { get; set; }

        // Навигационные свойства
        public virtual ICollection<ChargePointEntity> ChargePoints { get; set; } = new List<ChargePointEntity>();

        // Методы
        public void UpdateInfo(string name, string? description, string? address,
                              decimal? latitude, decimal? longitude, string? city,
                              string? region, string? postalCode)
        {
            Name = name;
            Description = description;
            Address = address;
            Latitude = latitude;
            Longitude = longitude;
            City = city;
            Region = region;
            PostalCode = postalCode;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatus(string status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
