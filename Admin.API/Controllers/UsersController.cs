using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Admin.API.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/admin/users")]
    public class UsersController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IApplicationDbContext context,
            IAuthService authService,
            ILogger<UsersController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.Id);

                var total = await query.CountAsync();
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Phone,
                        u.IsActive,
                        u.IsEmailConfirmed,
                        u.CreatedAt,
                        u.LastLoginAt,
                        Company = u.Company != null ? new
                        {
                            u.Company.Id,
                            u.Company.Name
                        } : null,
                        Roles = u.UserRoles.Select(ur => new
                        {
                            ur.Role.Id,
                            ur.Role.Name
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Users = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(new { Error = "User not found" });

                return Ok(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Phone,
                    user.IsActive,
                    user.IsEmailConfirmed,
                    user.CreatedAt,
                    user.LastLoginAt,
                    Company = user.Company != null ? new
                    {
                        user.Company.Id,
                        user.Company.Name,
                        user.Company.ContactEmail,
                        user.Company.ContactPhone
                    } : null,
                    Roles = user.UserRoles.Select(ur => new
                    {
                        ur.Role.Id,
                        ur.Role.Name,
                        ur.Role.Description,
                        Permissions = ur.Role.RolePermissions.Select(rp => new
                        {
                            rp.Permission.Id,
                            rp.Permission.Name,
                            rp.Permission.Resource,
                            rp.Permission.Action
                        }).ToList()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Проверяем, существует ли пользователь с таким email
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                    return BadRequest(new { Error = "User with this email already exists" });

                // Хэшируем пароль
                var (passwordHash, salt) = await _authService.HashPasswordAsync(request.Password);

                var user = new UserEntity
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    PasswordSalt = salt,
                    CompanyId = request.CompanyId,
                    IsActive = request.IsActive,
                    IsEmailConfirmed = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Назначаем роли
                if (request.RoleIds != null && request.RoleIds.Any())
                {
                    foreach (var roleId in request.RoleIds)
                    {
                        var role = await _context.Roles.FindAsync(roleId);
                        if (role != null)
                        {
                            _context.UserRoles.Add(new UserRoleEntity
                            {
                                UserId = user.Id,
                                RoleId = roleId
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("User created: {UserId} - {Email}", user.Id, user.Email);

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.IsActive,
                    user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { Error = "User not found" });

                user.UpdateProfile(request.FirstName, request.LastName, request.Phone);
                user.IsActive = request.IsActive;
                user.CompanyId = request.CompanyId;

                await _context.SaveChangesAsync();

                // Обновляем роли
                if (request.RoleIds != null)
                {
                    // Удаляем старые роли
                    var oldRoles = _context.UserRoles.Where(ur => ur.UserId == id);
                    _context.UserRoles.RemoveRange(oldRoles);

                    // Добавляем новые роли
                    foreach (var roleId in request.RoleIds)
                    {
                        var role = await _context.Roles.FindAsync(roleId);
                        if (role != null)
                        {
                            _context.UserRoles.Add(new UserRoleEntity
                            {
                                UserId = id,
                                RoleId = roleId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("User updated: {UserId}", id);

                return Ok(new { Message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { Error = "User not found" });

                // Мягкое удаление - деактивация
                user.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deactivated: {UserId}", id);

                return Ok(new { Message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { Error = "User not found" });

                var (passwordHash, salt) = await _authService.HashPasswordAsync(request.NewPassword);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = salt;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for user: {UserId}", id);

                return Ok(new { Message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class CreateUserRequest
        {
            public string Email { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Phone { get; set; }
            public string Password { get; set; } = string.Empty;
            public int? CompanyId { get; set; }
            public bool IsActive { get; set; } = true;
            public List<int>? RoleIds { get; set; }
        }

        public class UpdateUserRequest
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Phone { get; set; }
            public bool IsActive { get; set; }
            public int? CompanyId { get; set; }
            public List<int>? RoleIds { get; set; }
        }

        public class ResetPasswordRequest
        {
            public string NewPassword { get; set; } = string.Empty;
        }
    }
}
