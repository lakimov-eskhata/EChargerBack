using Domain.Entities.User;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(string email, string password);
        Task<AuthResult> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string token);
        Task<string> GenerateJwtTokenAsync(UserEntity user);
        Task<string> GenerateRefreshTokenAsync();
        Task<UserEntity?> GetUserByEmailAsync(string email);
        Task<bool> ValidatePasswordAsync(string password, string passwordHash, string? passwordSalt);
        Task<(string hash, string salt)> HashPasswordAsync(string password);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public UserEntity? User { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
