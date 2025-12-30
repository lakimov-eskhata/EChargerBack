using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Client.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/client/dashboard")]
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
                // Получаем ID компании из JWT токена
                var companyIdClaim = User.FindFirst("companyId")?.Value;
                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
                {
                    return BadRequest(new { Error = "Company ID not found in token" });
                }

                var now = DateTime.UtcNow;
                var today = now.Date;
                var weekStart = now.AddDays(-7);
                var monthStart = new DateTime(now.Year, now.Month, 1);

                // Станции компании
                var companyChargePoints = await _context.ChargePoints
                    .Where(cp => cp.CompanyId == companyId)
                    .ToListAsync();

                var onlineChargePoints = companyChargePoints.Count(cp => cp.Status == "Online");
                var offlineChargePoints = companyChargePoints.Count(cp => cp.Status != "Online");
                var activeChargePoints = companyChargePoints.Count(cp =>
                    cp.Status == "Online" || cp.Status == "Charging");

                // Транзакции сегодня
                var transactionsToday = await _context.Transactions
                    .CountAsync(t => t.ChargePoint.CompanyId == companyId && t.StartTimestamp.Date == today);

                // Транзакции за неделю
                var transactionsThisWeek = await _context.Transactions
                    .CountAsync(t => t.ChargePoint.CompanyId == companyId && t.StartTimestamp >= weekStart);

                // Транзакции за месяц
                var transactionsThisMonth = await _context.Transactions
                    .CountAsync(t => t.ChargePoint.CompanyId == companyId && t.StartTimestamp >= monthStart);

                // Потребленная энергия сегодня
                var energyConsumedToday = await _context.Transactions
                    .Where(t => t.ChargePoint.CompanyId == companyId && t.StartTimestamp.Date == today &&
                               t.MeterStop.HasValue && t.MeterStart.HasValue)
                    .SumAsync(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0;

                // Потребленная энергия за неделю
                var energyConsumedThisWeek = await _context.Transactions
                    .Where(t => t.ChargePoint.CompanyId == companyId && t.StartTimestamp >= weekStart &&
                               t.MeterStop.HasValue && t.MeterStart.HasValue)
                    .SumAsync(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0;

                // Активные транзакции
                var activeTransactions = await _context.Transactions
                    .CountAsync(t => t.ChargePoint.CompanyId == companyId && t.Status == 1); // Started

                // Доход за сегодня (предполагаемая логика - в реальном проекте нужна таблица тарифов)
                var revenueToday = energyConsumedToday * 0.25; // 0.25 руб/кВт·ч для примера
                var revenueThisWeek = energyConsumedThisWeek * 0.25;

                // Статистика по дням за неделю
                var dailyStats = await _context.Transactions
                    .Where(t => t.ChargePoint.CompanyId == companyId && t.StartTimestamp >= weekStart)
                    .GroupBy(t => t.StartTimestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Transactions = g.Count(),
                        EnergyConsumed = g.Where(t => t.MeterStop.HasValue && t.MeterStart.HasValue)
                                         .Sum(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0,
                        Revenue = (g.Where(t => t.MeterStop.HasValue && t.MeterStart.HasValue)
                                  .Sum(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0) * 0.25
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                var result = new
                {
                    ChargePoints = new
                    {
                        Total = companyChargePoints.Count,
                        Online = onlineChargePoints,
                        Offline = offlineChargePoints,
                        Active = activeChargePoints
                    },
                    Transactions = new
                    {
                        Today = transactionsToday,
                        ThisWeek = transactionsThisWeek,
                        ThisMonth = transactionsThisMonth,
                        Active = activeTransactions
                    },
                    Energy = new
                    {
                        ConsumedToday = energyConsumedToday,
                        ConsumedThisWeek = energyConsumedThisWeek
                    },
                    Revenue = new
                    {
                        Today = revenueToday,
                        ThisWeek = revenueThisWeek
                    },
                    DailyStats = dailyStats,
                    Timestamp = now
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats for company");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }
    }
}
