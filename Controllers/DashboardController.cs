using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequirePermission("Order.View")]
public sealed class DashboardController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public DashboardController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(targetDate.Year, targetDate.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var completedOrders = _db.Orders.AsNoTracking()
            .Where(x => x.Status == "completed");

        var summary = new DashboardSummaryDto
        {
            TodayRevenue = await completedOrders
                .Where(x => x.OrderDate == targetDate)
                .SumAsync(x => x.Amount),
            MonthRevenue = await completedOrders
                .Where(x => x.OrderDate >= monthStart && x.OrderDate < nextMonth)
                .SumAsync(x => x.Amount),
            MonthCommissionAmount = await completedOrders
                .Where(x => x.OrderDate >= monthStart && x.OrderDate < nextMonth)
                .SumAsync(x => x.CommissionAmount),
            MonthShareAmount = await _db.OrderMembers.AsNoTracking()
                .Where(x => x.Order.Status == "completed"
                    && x.Order.OrderDate >= monthStart
                    && x.Order.OrderDate < nextMonth)
                .SumAsync(x => x.ShareAmount),
            UnpaidOrderCount = await completedOrders
                .CountAsync(x => x.CustomerPaymentStatus == "unpaid"),
            CompletedOrderCount = await completedOrders
                .CountAsync(x => x.OrderDate >= monthStart && x.OrderDate < nextMonth)
        };

        return Ok(summary);
    }

    [HttpGet("ranking")]
    public async Task<ActionResult<List<MemberRankingDto>>> GetRanking(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var query = _db.OrderMembers.AsNoTracking()
            .Where(x => x.Order.Status == "completed");

        if (from.HasValue)
        {
            query = query.Where(x => x.Order.OrderDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Order.OrderDate <= to.Value);
        }

        var ranking = await query
            .GroupBy(x => new { x.UserId, x.User.Nickname })
            .Select(x => new MemberRankingDto
            {
                UserId = x.Key.UserId,
                Nickname = x.Key.Nickname,
                TotalShareAmount = x.Sum(v => v.ShareAmount),
                OrderCount = x.Select(v => v.OrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalShareAmount)
            .ThenByDescending(x => x.OrderCount)
            .ThenBy(x => x.Nickname)
            .ToListAsync();

        return Ok(ranking);
    }
}
