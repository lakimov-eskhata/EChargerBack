using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admin.API.Controllers
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.AuthenticateAsync(request.Email, request.Password);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                    return Unauthorized(new { Error = result.ErrorMessage });
                }

                // Проверяем, что пользователь имеет роль SuperAdmin
                if (!result.User?.UserRoles.Any(ur => ur.Role.Name == "SuperAdmin") ?? true)
                {
                    _logger.LogWarning("Access denied for user {UserId} - not a SuperAdmin", result.User?.Id);
                    return Forbid("Access denied. SuperAdmin role required.");
                }

                _logger.LogInformation("SuperAdmin login successful: {Email}", request.Email);

                return Ok(new
                {
                    result.Token,
                    result.ExpiresAt,
                    User = new
                    {
                        result.User?.Id,
                        result.User?.Email,
                        result.User?.FirstName,
                        result.User?.LastName,
                        Roles = result.User?.UserRoles.Select(ur => ur.Role.Name).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);

                if (!result.Success)
                {
                    return BadRequest(new { Error = result.ErrorMessage });
                }

                return Ok(new
                {
                    result.Token,
                    result.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                var result = await _authService.RevokeTokenAsync(request.Token);

                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RefreshTokenRequest
        {
            public string RefreshToken { get; set; } = string.Empty;
        }

        public class LogoutRequest
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}
