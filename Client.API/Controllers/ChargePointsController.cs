using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Client.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/client/charge-points")]
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
        public async Task<IActionResult> GetChargePoints()
        {
            try
            {
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var chargePoints = await _context.ChargePoints
                    .Where(cp => cp.CompanyId == companyId)
                    .Include(cp => cp.Connectors)
                    .Include(cp => cp.Transactions.OrderByDescending(t => t.StartTimestamp).Take(5))
                    .Select(cp => new
                    {
                        cp.Id,
                        cp.ChargePointId,
                        cp.Name,
                        cp.Status,
                        cp.ProtocolVersion,
                        cp.Vendor,
                        cp.Model,
                        cp.LastBootTime,
                        cp.LastHeartbeat,
                        ConnectorsCount = cp.Connectors.Count,
                        ActiveConnectors = cp.Connectors.Count(c => c.Status == "Charging"),
                        TotalTransactions = cp.Transactions.Count,
                        RecentTransactions = cp.Transactions.Select(t => new
                        {
                            t.Id,
                            t.TransactionId,
                            t.IdTag,
                            t.StartTimestamp,
                            t.StopTimestamp,
                            EnergyConsumed = t.GetEnergyConsumed(),
                            t.Status
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = chargePoints.Count,
                    ChargePoints = chargePoints
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting charge points for company");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChargePoint(int id)
        {
            try
            {
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var chargePoint = await _context.ChargePoints
                    .Include(cp => cp.Connectors)
                        .ThenInclude(c => c.ActiveTransaction)
                    .Include(cp => cp.Transactions.OrderByDescending(t => t.StartTimestamp).Take(10))
                    .FirstOrDefaultAsync(cp => cp.Id == id && cp.CompanyId == companyId);

                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                return Ok(new
                {
                    chargePoint.Id,
                    chargePoint.ChargePointId,
                    chargePoint.Name,
                    chargePoint.Status,
                    chargePoint.ProtocolVersion,
                    chargePoint.Vendor,
                    chargePoint.Model,
                    chargePoint.SerialNumber,
                    chargePoint.FirmwareVersion,
                    chargePoint.LastBootTime,
                    chargePoint.LastHeartbeat,
                    chargePoint.CreatedAt,
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
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var chargePoint = await _context.ChargePoints
                    .FirstOrDefaultAsync(cp => cp.Id == id && cp.CompanyId == companyId);

                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                var result = await _ocppService.ResetChargePointAsync(chargePoint.ChargePointId, request.Type);

                if (result)
                {
                    _logger.LogInformation("Reset command sent to charge point {ChargePointId} by company {CompanyId}",
                        chargePoint.ChargePointId, companyId);
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
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var chargePoint = await _context.ChargePoints
                    .FirstOrDefaultAsync(cp => cp.Id == id && cp.CompanyId == companyId);

                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                var result = await _ocppService.UnlockConnectorAsync(chargePoint.ChargePointId, connectorId);

                if (result)
                {
                    _logger.LogInformation("Unlock command sent to connector {ConnectorId} on charge point {ChargePointId} by company {CompanyId}",
                        connectorId, chargePoint.ChargePointId, companyId);
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
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var chargePoint = await _context.ChargePoints
                    .FirstOrDefaultAsync(cp => cp.Id == id && cp.CompanyId == companyId);

                if (chargePoint == null)
                    return NotFound(new { Error = "Charge point not found" });

                chargePoint.Name = request.Name;
                chargePoint.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Charge point updated: {ChargePointId} by company {CompanyId}", id, companyId);

                return Ok(new { Message = "Charge point updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating charge point {ChargePointId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class ResetRequest
        {
            public string Type { get; set; } = "Soft"; // Soft or Hard
        }

        public class UpdateChargePointRequest
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}
