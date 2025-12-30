using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Admin.API.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/admin/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IApplicationDbContext context,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = now.Date;
                var monthStart = new DateTime(now.Year, now.Month, 1);

                // Количество активных владельцев
                var activeOwners = await _context.Companies.CountAsync(c => c.IsActive);

                // Станции - онлайн/оффлайн
                var chargePoints = await _context.ChargePoints.ToListAsync();
                var onlineChargePoints = chargePoints.Count(cp => cp.Status == "Online");
                var offlineChargePoints = chargePoints.Count(cp => cp.Status != "Online");

                // Транзакции сегодня
                var transactionsToday = await _context.Transactions
                    .CountAsync(t => t.StartTimestamp.Date == today);

                // Транзакции за месяц
                var transactionsThisMonth = await _context.Transactions
                    .CountAsync(t => t.StartTimestamp >= monthStart);

                // Активные транзакции
                var activeTransactions = await _context.Transactions
                    .CountAsync(t => t.Status == 1); // Started status

                // Потребленная энергия сегодня
                var energyConsumedToday = await _context.Transactions
                    .Where(t => t.StartTimestamp.Date == today && t.MeterStop.HasValue && t.MeterStart.HasValue)
                    .SumAsync(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0;

                // Станции по протоколам
                var chargePointsByProtocol = await _context.ChargePoints
                    .GroupBy(cp => cp.ProtocolVersion)
                    .Select(g => new { Protocol = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Protocol ?? "Unknown", x => x.Count);

                // Коннекторы по статусам
                var connectorsByStatus = await _context.Connectors
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status ?? "Unknown", x => x.Count);

                // Последние ошибки (транзакции с ошибками за последние 24 часа)
                var recentErrors = await _context.Transactions
                    .Where(t => t.StartTimestamp >= now.AddDays(-1) && !string.IsNullOrEmpty(t.Reason))
                    .OrderByDescending(t => t.StartTimestamp)
                    .Take(5)
                    .Select(t => new
                    {
                        t.Id,
                        t.TransactionId,
                        ChargePointId = t.ChargePoint.ChargePointId,
                        t.Reason,
                        t.StartTimestamp
                    })
                    .ToListAsync();

                var result = new
                {
                    ActiveOwners = activeOwners,
                    ChargePoints = new
                    {
                        Total = chargePoints.Count,
                        Online = onlineChargePoints,
                        Offline = offlineChargePoints
                    },
                    Transactions = new
                    {
                        Today = transactionsToday,
                        ThisMonth = transactionsThisMonth,
                        Active = activeTransactions
                    },
                    EnergyConsumedToday = energyConsumedToday,
                    ChargePointsByProtocol = chargePointsByProtocol,
                    ConnectorsByStatus = connectorsByStatus,
                    RecentErrors = recentErrors,
                    Timestamp = now
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }
    }
}
