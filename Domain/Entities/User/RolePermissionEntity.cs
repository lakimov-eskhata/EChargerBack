namespace Domain.Entities.User
{
    public class RolePermissionEntity
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        // Навигационные свойства
        public virtual RoleEntity Role { get; set; } = null!;
        public virtual PermissionEntity Permission { get; set; } = null!;
    }
}
