using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Admin.API.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/admin/stations")]
    public class StationsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<StationsController> _logger;

        public StationsController(
            IApplicationDbContext context,
            ILogger<StationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetStations([FromQuery] StationsFilter filter)
        {
            try
            {
                var query = _context.Stations
                    .Include(s => s.Company)
                    .Include(s => s.ChargePoints)
                    .AsQueryable();

                // Фильтры
                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(s => s.Status == filter.Status);

                if (filter.CompanyId.HasValue)
                    query = query.Where(s => s.CompanyId == filter.CompanyId);

                if (!string.IsNullOrEmpty(filter.City))
                    query = query.Where(s => s.City.Contains(filter.City));

                if (!string.IsNullOrEmpty(filter.Search))
                {
                    query = query.Where(s =>
                        s.Name.Contains(filter.Search) ||
                        s.Address.Contains(filter.Search) ||
                        s.Description.Contains(filter.Search));
                }

                var total = await query.CountAsync();
                var stations = await query
                    .OrderBy(s => s.Id)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Description,
                        s.Address,
                        s.Latitude,
                        s.Longitude,
                        s.City,
                        s.Region,
                        s.PostalCode,
                        s.Status,
                        s.CreatedAt,
                        s.UpdatedAt,
                        Company = s.Company != null ? new
                        {
                            s.Company.Id,
                            s.Company.Name
                        } : null,
                        ChargePointsCount = s.ChargePoints.Count,
                        OnlineChargePointsCount = s.ChargePoints.Count(cp => cp.Status == "Online")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                    Stations = stations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stations");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStation(int id)
        {
            try
            {
                var station = await _context.Stations
                    .Include(s => s.Company)
                    .Include(s => s.ChargePoints)
                        .ThenInclude(cp => cp.Connectors)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (station == null)
                    return NotFound(new { Error = "Station not found" });

                return Ok(new
                {
                    station.Id,
                    station.Name,
                    station.Description,
                    station.Address,
                    station.Latitude,
                    station.Longitude,
                    station.City,
                    station.Region,
                    station.PostalCode,
                    station.Status,
                    station.CreatedAt,
                    station.UpdatedAt,
                    Company = station.Company != null ? new
                    {
                        station.Company.Id,
                        station.Company.Name,
                        station.Company.ContactEmail
                    } : null,
                    ChargePoints = station.ChargePoints.Select(cp => new
                    {
                        cp.Id,
                        cp.ChargePointId,
                        cp.Name,
                        cp.Status,
                        cp.ProtocolVersion,
                        cp.LastHeartbeat,
                        ConnectorsCount = cp.Connectors.Count
                    }).ToList(),
                    TotalChargePoints = station.ChargePoints.Count,
                    OnlineChargePoints = station.ChargePoints.Count(cp => cp.Status == "Online"),
                    TotalConnectors = station.ChargePoints.Sum(cp => cp.Connectors.Count)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting station {StationId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateStation([FromBody] CreateStationRequest request)
        {
            try
            {
                // Проверяем, существует ли компания
                if (request.CompanyId.HasValue)
                {
                    var company = await _context.Companies.FindAsync(request.CompanyId.Value);
                    if (company == null)
                        return BadRequest(new { Error = "Company not found" });
                }

                var station = new StationEntity
                {
                    Name = request.Name,
                    Description = request.Description,
                    Address = request.Address,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    City = request.City,
                    Region = request.Region,
                    PostalCode = request.PostalCode,
                    Status = request.Status ?? "Active",
                    CompanyId = request.CompanyId
                };

                _context.Stations.Add(station);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Station created: {StationId} - {StationName}", station.Id, station.Name);

                return CreatedAtAction(nameof(GetStation), new { id = station.Id }, new
                {
                    station.Id,
                    station.Name,
                    station.Address,
                    station.Status,
                    station.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating station");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStation(int id, [FromBody] UpdateStationRequest request)
        {
            try
            {
                var station = await _context.Stations.FindAsync(id);
                if (station == null)
                    return NotFound(new { Error = "Station not found" });

                station.UpdateInfo(
                    request.Name,
                    request.Description,
                    request.Address,
                    request.Latitude,
                    request.Longitude,
                    request.City,
                    request.Region,
                    request.PostalCode
                );

                if (!string.IsNullOrEmpty(request.Status))
                    station.UpdateStatus(request.Status);

                station.CompanyId = request.CompanyId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Station updated: {StationId}", id);

                return Ok(new { Message = "Station updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating station {StationId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStation(int id)
        {
            try
            {
                var station = await _context.Stations
                    .Include(s => s.ChargePoints)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (station == null)
                    return NotFound(new { Error = "Station not found" });

                // Проверяем, есть ли активные ChargePoint'ы
                if (station.ChargePoints.Any())
                    return BadRequest(new { Error = "Cannot delete station with active charge points" });

                _context.Stations.Remove(station);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Station deleted: {StationId}", id);

                return Ok(new { Message = "Station deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting station {StationId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class StationsFilter
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public string? Status { get; set; }
            public int? CompanyId { get; set; }
            public string? City { get; set; }
            public string? Search { get; set; }
        }

        public class CreateStationRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? Address { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string? City { get; set; }
            public string? Region { get; set; }
            public string? PostalCode { get; set; }
            public string? Status { get; set; } = "Active";
            public int? CompanyId { get; set; }
        }

        public class UpdateStationRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? Address { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string? City { get; set; }
            public string? Region { get; set; }
            public string? PostalCode { get; set; }
            public string? Status { get; set; }
            public int? CompanyId { get; set; }
        }
    }
}
