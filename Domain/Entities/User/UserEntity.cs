namespace Domain.Entities.User
{
    public class UserEntity
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string? PasswordSalt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsEmailConfirmed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Внешние ключи
        public int? CompanyId { get; set; }

        // Навигационные свойства
        public virtual CompanyEntity? Company { get; set; }
        public virtual ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();

        // Методы
        public string GetFullName()
        {
            return $"{FirstName} {LastName}".Trim();
        }

        public void UpdateProfile(string? firstName, string? lastName, string? phone)
        {
            FirstName = firstName;
            LastName = lastName;
            Phone = phone;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateLoginTime()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ConfirmEmail()
        {
            IsEmailConfirmed = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
