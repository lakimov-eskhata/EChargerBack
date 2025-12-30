using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Admin.API.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/admin/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            IApplicationDbContext context,
            ILogger<TransactionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions([FromQuery] TransactionsFilter filter)
        {
            try
            {
                var query = _context.Transactions
                    .Include(t => t.ChargePoint)
                        .ThenInclude(cp => cp.Company)
                    .AsQueryable();

                // Фильтры
                if (filter.StartDate.HasValue)
                    query = query.Where(t => t.StartTimestamp >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(t => t.StartTimestamp <= filter.EndDate.Value);

                if (filter.CompanyId.HasValue)
                    query = query.Where(t => t.ChargePoint.CompanyId == filter.CompanyId);

                if (filter.ChargePointId.HasValue)
                    query = query.Where(t => t.ChargePointId == filter.ChargePointId);

                if (!string.IsNullOrEmpty(filter.IdTag))
                    query = query.Where(t => t.IdTag.Contains(filter.IdTag));

                if (filter.Status.HasValue)
                    query = query.Where(t => t.Status == filter.Status);

                var total = await query.CountAsync();
                var transactions = await query
                    .OrderByDescending(t => t.StartTimestamp)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.TransactionId,
                        t.IdTag,
                        t.ConnectorId,
                        t.StartTimestamp,
                        t.StopTimestamp,
                        t.MeterStart,
                        t.MeterStop,
                        EnergyConsumed = t.GetEnergyConsumed(),
                        t.Reason,
                        t.Status,
                        ChargePoint = new
                        {
                            t.ChargePoint.Id,
                            t.ChargePoint.ChargePointId,
                            t.ChargePoint.Name
                        },
                        Company = t.ChargePoint.Company != null ? new
                        {
                            t.ChargePoint.Company.Id,
                            t.ChargePoint.Company.Name
                        } : null,
                        Duration = t.GetDuration()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Total = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                    Transactions = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(int id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.ChargePoint)
                        .ThenInclude(cp => cp.Company)
                    .Include(t => t.MeterValues.OrderByDescending(mv => mv.Timestamp))
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transaction == null)
                    return NotFound(new { Error = "Transaction not found" });

                return Ok(new
                {
                    transaction.Id,
                    transaction.TransactionId,
                    transaction.IdTag,
                    transaction.ParentIdTag,
                    transaction.ConnectorId,
                    transaction.StartTimestamp,
                    transaction.StopTimestamp,
                    transaction.StopValueTimestamp,
                    transaction.MeterStart,
                    transaction.MeterStop,
                    transaction.MeterValue,
                    EnergyConsumed = transaction.GetEnergyConsumed(),
                    transaction.Reason,
                    transaction.Status,
                    Duration = transaction.GetDuration(),
                    ChargePoint = new
                    {
                        transaction.ChargePoint.Id,
                        transaction.ChargePoint.ChargePointId,
                        transaction.ChargePoint.Name,
                        transaction.ChargePoint.Vendor,
                        transaction.ChargePoint.Model
                    },
                    Company = transaction.ChargePoint.Company != null ? new
                    {
                        transaction.ChargePoint.Company.Id,
                        transaction.ChargePoint.Company.Name
                    } : null,
                    MeterValues = transaction.MeterValues.Select(mv => new
                    {
                        mv.Id,
                        mv.Timestamp,
                        mv.Value,
                        mv.Context,
                        mv.Format,
                        mv.Measurand,
                        mv.Phase,
                        mv.Location,
                        mv.Unit
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction {TransactionId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetTransactionsStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var query = _context.Transactions.Where(t => t.StartTimestamp >= start && t.StartTimestamp <= end);

                var stats = new
                {
                    TotalTransactions = await query.CountAsync(),
                    CompletedTransactions = await query.CountAsync(t => t.Status == 2), // Completed
                    ActiveTransactions = await query.CountAsync(t => t.Status == 1), // Started
                    FailedTransactions = await query.CountAsync(t => !string.IsNullOrEmpty(t.Reason)),

                    TotalEnergyConsumed = await query
                        .Where(t => t.MeterStop.HasValue && t.MeterStart.HasValue)
                        .SumAsync(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0,

                    AverageTransactionDuration = await query
                        .Where(t => t.StopTimestamp.HasValue)
                        .AverageAsync(t => (t.StopTimestamp - t.StartTimestamp).Value.TotalMinutes),

                    TransactionsByDay = await query
                        .GroupBy(t => t.StartTimestamp.Date)
                        .Select(g => new
                        {
                            Date = g.Key,
                            Count = g.Count(),
                            EnergyConsumed = g.Where(t => t.MeterStop.HasValue && t.MeterStart.HasValue)
                                             .Sum(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync(),

                    TopChargePoints = await query
                        .GroupBy(t => new { t.ChargePoint.Id, t.ChargePoint.ChargePointId, t.ChargePoint.Name })
                        .Select(g => new
                        {
                            ChargePointId = g.Key.Id,
                            ChargePointName = g.Key.ChargePointId,
                            DisplayName = g.Key.Name,
                            TransactionCount = g.Count(),
                            EnergyConsumed = g.Where(t => t.MeterStop.HasValue && t.MeterStart.HasValue)
                                             .Sum(t => (double?)(t.MeterStop - t.MeterStart)) ?? 0
                        })
                        .OrderByDescending(x => x.TransactionCount)
                        .Take(10)
                        .ToListAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions stats");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportTransactions([FromQuery] TransactionsFilter filter)
        {
            try
            {
                var query = _context.Transactions
                    .Include(t => t.ChargePoint)
                        .ThenInclude(cp => cp.Company)
                    .AsQueryable();

                // Применяем те же фильтры
                if (filter.StartDate.HasValue)
                    query = query.Where(t => t.StartTimestamp >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(t => t.StartTimestamp <= filter.EndDate.Value);

                if (filter.CompanyId.HasValue)
                    query = query.Where(t => t.ChargePoint.CompanyId == filter.CompanyId);

                if (filter.ChargePointId.HasValue)
                    query = query.Where(t => t.ChargePointId == filter.ChargePointId);

                var transactions = await query
                    .OrderByDescending(t => t.StartTimestamp)
                    .Take(10000) // Ограничение для экспорта
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.IdTag,
                        ChargePointId = t.ChargePoint.ChargePointId,
                        ChargePointName = t.ChargePoint.Name,
                        CompanyName = t.ChargePoint.Company != null ? t.ChargePoint.Company.Name : "",
                        t.ConnectorId,
                        t.StartTimestamp,
                        t.StopTimestamp,
                        t.MeterStart,
                        t.MeterStop,
                        EnergyConsumed = t.GetEnergyConsumed(),
                        t.Reason,
                        Status = t.Status == 1 ? "Started" : t.Status == 2 ? "Completed" : "Other"
                    })
                    .ToListAsync();

                // В реальном приложении здесь был бы код для генерации CSV или Excel файла
                // Для упрощения возвращаем JSON
                return Ok(new
                {
                    Message = "Export data retrieved successfully",
                    TotalRecords = transactions.Count,
                    Data = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        // DTOs
        public class TransactionsFilter
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? CompanyId { get; set; }
            public int? ChargePointId { get; set; }
            public string? IdTag { get; set; }
            public int? Status { get; set; }
        }
    }
}
