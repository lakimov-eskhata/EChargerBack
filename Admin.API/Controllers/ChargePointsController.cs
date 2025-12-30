using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Admin.API.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/admin/charge-points")]
    public class ChargePointsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IOCPPService _ocppService;
        private readonly ILogger<ChargePointsController> _logger;

        public ChargePointsController(
            IApplicationDbContext context,
            IOCPPService ocppService,
            ILogger<ChargePointsController> logger)
        {
            _context = context;
            _ocppService = ocppService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetChargePoints([FromQuery] ChargePointsFilter filter)
        {
            try
            {
                var query = _context.ChargePoints
                    .Include(cp => cp.Station)
                        .ThenInclude(s => s.Company)
                    .Include(cp => cp.Connectors)
                    .Include(cp => cp.Transactions)
                    .AsQueryable();

                // Фильтры
                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(cp => cp.Status == filter.Status);

                if (!string.IsNullOrEmpty(filter.ProtocolVersion))
                    query = query.Where(cp => cp.ProtocolVersion == filter.ProtocolVersion);

                if (filter.StationId.HasValue)
                    query = query.Where(cp => cp.StationId == filter.StationId);

                if (filter.CompanyId.HasValue)
                    query = query.Where(cp => cp.Station.CompanyId == filter.CompanyId);

                if (!string.IsNullOrEmpty(filter.Search))
                {
                    query = query.Where(cp =>
                        cp.ChargePointId.Contains(filter.Search) ||
                        cp.Name.Contains(filter.Search) ||
                        cp.SerialNumber.Contains(filter.Search));
                }

                var total = await query.CountAsync();
                var chargePoints = await query
                    .OrderBy(cp => cp.Id)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(cp => new
                    {
                        cp.Id,
                        cp.ChargePointId,
                        cp.Name,
                        cp.ProtocolVersion,
                        cp.Vendor,
                        cp.Model,
                        cp.SerialNumber,
                        cp.Status,
                        cp.ConnectorCount,
                        cp.LastBootTime,
                        cp.LastHeartbeat,
                        cp.CreatedAt,
                        Station = cp.Station != null ? new
                        {
                            cp.Station.Id,
                            cp.Station.Name,
                            cp.Station.Address,
                            Company = cp.Station.Company != null ? new
                            {
                                cp.Station.Company.Id,
                                cp.Station.Company.Name
                            } : null
                        } : null,
                        ConnectorsCount = cp.Connectors.Count,
                        ActiveTransactionsCount = cp.Transactions.Count(t => t.Status == 1), // Started
                        TotalTransactionsCount = cp.Transactions.Count
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                    ChargePoints = chargePoints
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting charge points");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChargePoint(int id)
        {
            try
            {
                var chargePoint = await _context.ChargePoints
                    .Include(cp => cp.Company)
                    .Include(cp => cp.Connectors)
                        .ThenInclude(c => c.ActiveTransaction)
                    .Include(cp => cp.Transactions.OrderByDescending(t => t.StartTimestamp).Take(10))
                    .FirstOrDefaultAsync(cp => cp.Id == id);

                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                return Ok(new
                {
                    chargePoint.Id,
                    chargePoint.ChargePointId,
                    chargePoint.Name,
                    chargePoint.ProtocolVersion,
                    chargePoint.Vendor,
                    chargePoint.Model,
                    chargePoint.SerialNumber,
                    chargePoint.FirmwareVersion,
                    chargePoint.Status,
                    chargePoint.ConnectorCount,
                    chargePoint.HeartbeatInterval,
                    chargePoint.MeterType,
                    chargePoint.MeterSerialNumber,
                    chargePoint.Iccid,
                    chargePoint.Imsi,
                    chargePoint.LastBootTime,
                    chargePoint.LastHeartbeat,
                    chargePoint.CreatedAt,
                    chargePoint.UpdatedAt,
                    Company = chargePoint.Company != null ? new
                    {
                        chargePoint.Company.Id,
                        chargePoint.Company.Name
                    } : null,
                    Connectors = chargePoint.Connectors.Select(c => new
                    {
                        c.Id,
                        c.ConnectorId,
                        c.Status,
                        c.ErrorCode,
                        c.Info,
                        c.StatusTimestamp,
                        c.MeterValue,
                        ActiveTransactionId = c.ActiveTransaction?.TransactionId
                    }).ToList(),
                    RecentTransactions = chargePoint.Transactions.Select(t => new
                    {
                        t.Id,
                        t.TransactionId,
                        t.IdTag,
                        t.StartTimestamp,
                        t.StopTimestamp,
                        t.MeterStart,
                        t.MeterStop,
                        EnergyConsumed = t.GetEnergyConsumed(),
                        t.Status,
                        t.Reason
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting charge point {ChargePointId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("{id}/reset")]
        public async Task<IActionResult> ResetChargePoint(int id, [FromBody] ResetRequest request)
        {
            try
            {
                var chargePoint = await _context.ChargePoints.FindAsync(id);
                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                var result = await _ocppService.ResetChargePointAsync(chargePoint.ChargePointId, request.Type);

                if (result)
                {
                    _logger.LogInformation("Reset command sent to charge point {ChargePointId}", chargePoint.ChargePointId);
                    return Ok(new { Message = "Reset command sent successfully" });
                }
                else
                {
                    return BadRequest(new { Error = "Failed to send reset command" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting charge point {ChargePointId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("{id}/unlock-connector/{connectorId}")]
        public async Task<IActionResult> UnlockConnector(int id, int connectorId)
        {
            try
            {
                var chargePoint = await _context.ChargePoints.FindAsync(id);
                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                var result = await _ocppService.UnlockConnectorAsync(chargePoint.ChargePointId, connectorId);

                if (result)
                {
                    _logger.LogInformation("Unlock command sent to connector {ConnectorId} on charge point {ChargePointId}",
                        connectorId, chargePoint.ChargePointId);
                    return Ok(new { Message = "Unlock command sent successfully" });
                }
                else
                {
                    return BadRequest(new { Error = "Failed to send unlock command" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking connector {ConnectorId} on charge point {ChargePointId}", connectorId, id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChargePoint(int id, [FromBody] UpdateChargePointRequest request)
        {
            try
            {
                var chargePoint = await _context.ChargePoints.FindAsync(id);
                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                chargePoint.Name = request.Name;
                chargePoint.CompanyId = request.CompanyId;
                chargePoint.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Charge point updated: {ChargePointId}", id);

                return Ok(new { Message = "Charge point updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating charge point {ChargePointId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetChargePointsStats()
        {
            try
            {
                var chargePoints = await _context.ChargePoints.ToListAsync();
                var connectors = await _context.Connectors.ToListAsync();

                var stats = new
                {
                    TotalChargePoints = chargePoints.Count,
                    OnlineChargePoints = chargePoints.Count(cp => cp.Status == "Online"),
                    OfflineChargePoints = chargePoints.Count(cp => cp.Status != "Online"),
                    FaultedChargePoints = chargePoints.Count(cp => cp.Status == "Faulted"),

                    ConnectorsByStatus = connectors
                        .GroupBy(c => c.Status)
                        .Select(g => new { Status = g.Key, Count = g.Count() })
                        .ToDictionary(x => x.Status ?? "Unknown", x => x.Count),

                    ChargePointsByProtocol = chargePoints
                        .GroupBy(cp => cp.ProtocolVersion)
                        .Select(g => new { Protocol = g.Key, Count = g.Count() })
                        .ToDictionary(x => x.Protocol ?? "Unknown", x => x.Count),

                    ChargePointsByVendor = chargePoints
                        .GroupBy(cp => cp.Vendor)
                        .Select(g => new { Vendor = g.Key, Count = g.Count() })
                        .ToDictionary(x => x.Vendor ?? "Unknown", x => x.Count)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting charge points stats");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class ChargePointsFilter
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public string? Status { get; set; }
            public string? ProtocolVersion { get; set; }
            public int? StationId { get; set; }
            public int? CompanyId { get; set; }
            public string? Search { get; set; }
        }

        public class ResetRequest
        {
            public string Type { get; set; } = "Soft"; // Soft or Hard
        }

        public class UpdateChargePointRequest
        {
            public string Name { get; set; } = string.Empty;
            public int? StationId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChargePoint([FromBody] CreateChargePointRequest request)
        {
            try
            {
                // Проверяем, существует ли станция
                if (request.StationId.HasValue)
                {
                    var station = await _context.Stations.FindAsync(request.StationId.Value);
                    if (station == null)
                        return BadRequest(new { Error = "Station not found" });
                }

                // Проверяем, не занят ли ChargePointId
                var existingChargePoint = await _context.ChargePoints
                    .FirstOrDefaultAsync(cp => cp.ChargePointId == request.ChargePointId);

                if (existingChargePoint != null)
                    return BadRequest(new { Error = "ChargePointId already exists" });

                var chargePoint = new ChargePointEntity
                {
                    ChargePointId = request.ChargePointId,
                    Name = request.Name,
                    ProtocolVersion = request.ProtocolVersion ?? "1.6",
                    Vendor = request.Vendor,
                    Model = request.Model,
                    SerialNumber = request.SerialNumber,
                    FirmwareVersion = request.FirmwareVersion,
                    MeterType = request.MeterType,
                    MeterSerialNumber = request.MeterSerialNumber,
                    StationId = request.StationId
                };

                _context.ChargePoints.Add(chargePoint);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Charge point created: {ChargePointId} with ID {Id}", chargePoint.ChargePointId, chargePoint.Id);

                return CreatedAtAction(nameof(GetChargePoint), new { id = chargePoint.Id }, new
                {
                    chargePoint.Id,
                    chargePoint.ChargePointId,
                    chargePoint.Name,
                    chargePoint.ProtocolVersion,
                    chargePoint.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating charge point");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class CreateChargePointRequest
        {
            public string ChargePointId { get; set; } = string.Empty; // Обязательный уникальный ID для OCPP
            public string? Name { get; set; }
            public string? ProtocolVersion { get; set; } = "1.6";
            public string? Vendor { get; set; }
            public string? Model { get; set; }
            public string? SerialNumber { get; set; }
            public string? FirmwareVersion { get; set; }
            public string? MeterType { get; set; }
            public string? MeterSerialNumber { get; set; }
            public int? StationId { get; set; }
        }
    }
}
