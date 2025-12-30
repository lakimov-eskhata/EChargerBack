using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Client.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/client/employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            IApplicationDbContext context,
            IAuthService authService,
            ILogger<EmployeesController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var employees = await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.Id)
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
                        Roles = u.UserRoles.Select(ur => new
                        {
                            ur.Role.Id,
                            ur.Role.Name
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = employees.Count,
                    Employees = employees
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees for company");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeRequest request)
        {
            try
            {
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

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
                    CompanyId = companyId,
                    IsActive = true,
                    IsEmailConfirmed = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Назначаем роль сотрудника компании
                var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "CompanyEmployee");
                if (employeeRole != null)
                {
                    _context.UserRoles.Add(new UserRoleEntity
                    {
                        UserId = user.Id,
                        RoleId = employeeRole.Id
                    });
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Employee added to company {CompanyId}: {EmployeeId} - {Email}", companyId, user.Id, user.Email);

                return CreatedAtAction(nameof(GetEmployees), new
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
                _logger.LogError(ex, "Error adding employee to company");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                    return NotFound(new { Error = "Employee not found" });

                user.UpdateProfile(request.FirstName, request.LastName, request.Phone);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee updated: {EmployeeId} in company {CompanyId}", id, companyId);

                return Ok(new { Message = "Employee updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveEmployee(int id)
        {
            try
            {
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                    return NotFound(new { Error = "Employee not found" });

                // Мягкое удаление - деактивация
                user.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee deactivated: {EmployeeId} in company {CompanyId}", id, companyId);

                return Ok(new { Message = "Employee removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing employee {EmployeeId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetEmployeePassword(int id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                    return NotFound(new { Error = "Employee not found" });

                var (passwordHash, salt) = await _authService.HashPasswordAsync(request.NewPassword);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = salt;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for employee {EmployeeId} in company {CompanyId}", id, companyId);

                return Ok(new { Message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for employee {EmployeeId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class AddEmployeeRequest
        {
            public string Email { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Phone { get; set; }
            public string Password { get; set; } = string.Empty;
        }

        public class UpdateEmployeeRequest
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Phone { get; set; }
        }

        public class ResetPasswordRequest
        {
            public string NewPassword { get; set; } = string.Empty;
        }
    }
}
