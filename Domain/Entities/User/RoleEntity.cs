namespace Domain.Entities.User
{
    public class RoleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; } = false; // Системные роли нельзя удалять
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public virtual ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();
        public virtual ICollection<RolePermissionEntity> RolePermissions { get; set; } = new List<RolePermissionEntity>();

        // Системные роли
        public static readonly RoleEntity SuperAdmin = new()
        {
            Id = 1,
            Name = "SuperAdmin",
            Description = "Суперадминистратор системы",
            IsSystemRole = true
        };

        public static readonly RoleEntity CompanyOwner = new()
        {
            Id = 2,
            Name = "CompanyOwner",
            Description = "Владелец компании",
            IsSystemRole = true
        };

        public static readonly RoleEntity CompanyEmployee = new()
        {
            Id = 3,
            Name = "CompanyEmployee",
            Description = "Сотрудник компании",
            IsSystemRole = true
        };
    }
}
