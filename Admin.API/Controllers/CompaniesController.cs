using Application.Common.Interfaces;
using Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Admin.API.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/admin/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(
            IApplicationDbContext context,
            ILogger<CompaniesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Companies
                    .Include(c => c.Users)
                    .Include(c => c.ChargePoints)
                    .OrderBy(c => c.Id);

                var total = await query.CountAsync();
                var companies = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.ContactEmail,
                        c.ContactPhone,
                        c.Address,
                        c.Balance,
                        c.IsActive,
                        c.CreatedAt,
                        c.UpdatedAt,
                        UsersCount = c.Users.Count,
                        ChargePointsCount = c.ChargePoints.Count
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Companies = companies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Users)
                        .ThenInclude(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                    .Include(c => c.ChargePoints)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                    return NotFound(new { Error = "Company not found" });

                return Ok(new
                {
                    company.Id,
                    company.Name,
                    company.Description,
                    company.ContactEmail,
                    company.ContactPhone,
                    company.Address,
                    company.Balance,
                    company.IsActive,
                    company.CreatedAt,
                    company.UpdatedAt,
                    Users = company.Users.Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.IsActive,
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                    }).ToList(),
                    ChargePoints = company.ChargePoints.Select(cp => new
                    {
                        cp.Id,
                        cp.ChargePointId,
                        cp.Name,
                        cp.Status,
                        cp.ProtocolVersion,
                        cp.LastHeartbeat
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company {CompanyId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
        {
            try
            {
                // Проверяем, существует ли компания с таким именем
                var existingCompany = await _context.Companies.FirstOrDefaultAsync(c => c.Name == request.Name);
                if (existingCompany != null)
                    return BadRequest(new { Error = "Company with this name already exists" });

                var company = new CompanyEntity
                {
                    Name = request.Name,
                    Description = request.Description,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    Balance = request.InitialBalance,
                    IsActive = request.IsActive
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company created: {CompanyId} - {CompanyName}", company.Id, company.Name);

                return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, new
                {
                    company.Id,
                    company.Name,
                    company.IsActive,
                    company.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyRequest request)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                    return NotFound(new { Error = "Company not found" });

                company.UpdateInfo(request.Name, request.Description, request.ContactEmail, request.ContactPhone, request.Address);
                company.IsActive = request.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company updated: {CompanyId}", id);

                return Ok(new { Message = "Company updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Users)
                    .Include(c => c.ChargePoints)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                    return NotFound(new { Error = "Company not found" });

                // Проверяем, есть ли активные пользователи или станции
                if (company.Users.Any(u => u.IsActive) || company.ChargePoints.Any())
                    return BadRequest(new { Error = "Cannot delete company with active users or charge points" });

                // Мягкое удаление - деактивация
                company.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company deactivated: {CompanyId}", id);

                return Ok(new { Message = "Company deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating company {CompanyId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("{id}/adjust-balance")]
        public async Task<IActionResult> AdjustBalance(int id, [FromBody] AdjustBalanceRequest request)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                    return NotFound(new { Error = "Company not found" });

                company.UpdateBalance(request.Amount);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Balance adjusted for company {CompanyId}: {Amount}", id, request.Amount);

                return Ok(new
                {
                    Message = "Balance adjusted successfully",
                    NewBalance = company.Balance
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting balance for company {CompanyId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class CreateCompanyRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? ContactEmail { get; set; }
            public string? ContactPhone { get; set; }
            public string? Address { get; set; }
            public decimal InitialBalance { get; set; } = 0;
            public bool IsActive { get; set; } = true;
        }

        public class UpdateCompanyRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? ContactEmail { get; set; }
            public string? ContactPhone { get; set; }
            public string? Address { get; set; }
            public bool IsActive { get; set; }
        }

        public class AdjustBalanceRequest
        {
            public decimal Amount { get; set; }
        }
    }
}
