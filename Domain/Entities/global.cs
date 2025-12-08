namespace Domain.Entities
{
    // Базовый класс для всех сущностей
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
    
    // Компания (владелец станций)
    public class CompanyEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string INN { get; set; } = string.Empty; // Идентификационный номер
        public string Address { get; set; } = string.Empty;

        public virtual ICollection<StationEntity> Stations { get; set; } = new List<StationEntity>();
        public virtual ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
    }

    // Страна
    public class CountryEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string IsoCode { get; set; } = string.Empty;

        public virtual ICollection<RegionEntity> Regions { get; set; } = new List<RegionEntity>();
    }

    // Регион
    public class RegionEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public Guid CountryId { get; set; }
        public virtual CountryEntity Country { get; set; } = null!;

        public virtual ICollection<CityEntity> Cities { get; set; } = new List<CityEntity>();
    }

    // Город
    public class CityEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public Guid RegionId { get; set; }
        public virtual RegionEntity Region { get; set; } = null!;

        public virtual ICollection<StationEntity> Stations { get; set; } = new List<StationEntity>();
    }

    // Станция (заправка)
    public class StationEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public Guid CityId { get; set; }
        public virtual CityEntity City { get; set; } = null!;

        public Guid CompanyId { get; set; }
        public virtual CompanyEntity Company { get; set; } = null!;

        public virtual ICollection<ChargePointEntity> ChargePoints { get; set; } = new List<ChargePointEntity>();
        public virtual ICollection<StationUsersEntity> StationUsers { get; set; } = new List<StationUsersEntity>();
    }

    // Зарядные устройства
    public class ChargePointEntity : BaseEntity
    {
        public string ChargePointId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; }
        public string? Payload { get; set; }

        public Guid StationId { get; set; }
        public virtual StationEntity Station { get; set; } = null!;

        public virtual ICollection<ConnectorEntity> Connectors { get; set; } = new List<ConnectorEntity>();
    }

    // Коннектор
    public class ConnectorEntity : BaseEntity
    {
        public int ConnectorId { get; set; }
        public string Status { get; set; } = string.Empty;

        public Guid ChargePointId { get; set; }
        public virtual ChargePointEntity ChargePoint { get; set; } = null!;
    }

    // Пользователи
    public abstract class UserEntity : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public Guid RoleId { get; set; }
        public virtual RoleEntity Role { get; set; } = null!;
    }

    public class AdminUserEntity : UserEntity
    {
        public string AdminLevel { get; set; } = "System";
    }

    public class ClientUserEntity : UserEntity
    {
        public Guid CompanyId { get; set; }
        public virtual CompanyEntity Company { get; set; } = null!;
    }

    // Роли и разрешения
    public class RoleEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<PermissionEntity> Permissions { get; set; } = new List<PermissionEntity>();
    }

    public class PermissionEntity : BaseEntity
    {
        public string Code { get; set; } = string.Empty; // Например: "CAN_EDIT_STATION"
        public string Description { get; set; } = string.Empty;
    }

    // Пользователи, привязанные к станции
    public class StationUsersEntity : BaseEntity
    {
        public Guid StationId { get; set; }
        public virtual StationEntity Station { get; set; } = null!;

        public Guid UserId { get; set; }
        public virtual UserEntity User { get; set; } = null!;
    }

    // История зарядок
    public class HistoryChargesEntity : BaseEntity
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public Guid StationId { get; set; }
        public virtual StationEntity Station { get; set; } = null!;

        public double ConsumedKWh { get; set; }
        public decimal PricePerKWh { get; set; }
        public decimal TotalPaid { get; set; }
        public string Status { get; set; } = "Completed"; // Completed / Failed / Canceled
    }

    // Динамические тарифы
    public class DynamicTrafficPriceEntity : BaseEntity
    {
        public Guid StationId { get; set; }
        public virtual StationEntity Station { get; set; } = null!;

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal PricePerKWh { get; set; }
    }
}
