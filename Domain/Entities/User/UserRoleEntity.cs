namespace Domain.Entities.User
{
    public class UserRoleEntity
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        // Навигационные свойства
        public virtual UserEntity User { get; set; } = null!;
        public virtual RoleEntity Role { get; set; } = null!;
    }
}
