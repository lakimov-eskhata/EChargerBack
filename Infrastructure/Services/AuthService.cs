using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationDbContext _context;

        public AuthService(IConfiguration configuration, IApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AuthResult> AuthenticateAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid credentials"
                };
            }

            var isPasswordValid = await ValidatePasswordAsync(password, user.PasswordHash, user.PasswordSalt);
            if (!isPasswordValid)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid credentials"
                };
            }

            var token = await GenerateJwtTokenAsync(user);
            var refreshToken = await GenerateRefreshTokenAsync();

            user.UpdateLoginTime();
            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                User = user,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            // В реальном приложении refresh токены должны храниться в БД с истечением срока
            // Для упрощения возвращаем ошибку
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Refresh token functionality not implemented"
            };
        }

        public Task<bool> RevokeTokenAsync(string token)
        {
            // В реальном приложении нужно добавить токен в blacklist
            return Task.FromResult(true);
        }

        public async Task<string> GenerateJwtTokenAsync(UserEntity user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? "default-secret-key-for-development";
            var issuer = jwtSettings["Issuer"] ?? "echarger-api";
            var audience = jwtSettings["Audience"] ?? "echarger-client";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("userId", user.Id.ToString()),
                new Claim("companyId", user.CompanyId?.ToString() ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            // Добавляем роли пользователя
            if (user.UserRoles.Any())
            {
                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> GenerateRefreshTokenAsync()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Task.FromResult(Convert.ToBase64String(randomBytes));
        }

        public async Task<UserEntity?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<bool> ValidatePasswordAsync(string password, string passwordHash, string? passwordSalt)
        {
            if (string.IsNullOrEmpty(passwordSalt))
            {
                // Для простоты, если нет соли, сравниваем напрямую (для миграции пользователей)
                return password == passwordHash;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(passwordSalt));
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return computedHash == passwordHash;
        }

        public async Task<(string hash, string salt)> HashPasswordAsync(string password)
        {
            var salt = GenerateSalt();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return (hash, salt);
        }

        private string GenerateSalt()
        {
            var saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }
    }
}
