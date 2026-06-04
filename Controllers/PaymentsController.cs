using EAPlaymateGroup.Common;
using System.Text.Json;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public PaymentsController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments(
        [FromQuery] string? payMonth,
        [FromQuery] string? status)
    {
        var query = _db.Payments.AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(payMonth))
        {
            query = query.Where(x => x.PayMonth == payMonth);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.PaymentStatus == status);
        }

        var payments = await query
            .OrderByDescending(x => x.PayMonth)
            .ThenBy(x => x.User.Nickname)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(int id)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        return payment is null ? NotFound() : Ok(ToDto(payment));
    }

    [HttpPost("generate-monthly")]
    public async Task<ActionResult<List<PaymentDto>>> GenerateMonthlyPayments(GenerateMonthlyPaymentsRequestDto request)
    {
        if (!TryParsePayMonth(request.PayMonth, out var monthStart))
        {
            return ApiErrors.BadRequest("invalid_pay_month", "PayMonth must use yyyy-MM format.");
        }

        var nextMonth = monthStart.AddMonths(1);

        var incomeRows = await _db.OrderMembers.AsNoTracking()
            .Where(x => x.Order.Status == "completed"
                && x.Order.OrderDate >= monthStart
                && x.Order.OrderDate < nextMonth)
            .Select(x => new
            {
                x.UserId,
                x.User.Nickname,
                x.OrderId,
                x.Order.OrderNo,
                x.Order.OrderDate,
                x.Order.Amount,
                x.Order.CommissionAmount,
                x.Role,
                x.ShareAmount
            })
            .ToListAsync();

        var groupedRows = incomeRows
            .GroupBy(x => new { x.UserId, x.Nickname })
            .ToList();

        if (groupedRows.Count == 0)
        {
            return Ok(new List<PaymentDto>());
        }

        var userIds = groupedRows.Select(x => x.Key.UserId).ToList();
        var existingPayments = await _db.Payments
            .Where(x => x.PayMonth == request.PayMonth && userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);

        foreach (var group in groupedRows)
        {
            var expectedAmount = group.Sum(x => x.ShareAmount);
            var snapshot = new
            {
                pay_month = request.PayMonth,
                user_id = group.Key.UserId,
                nickname = group.Key.Nickname,
                expected_amount = expectedAmount,
                orders = group
                    .OrderBy(x => x.OrderDate)
                    .ThenBy(x => x.OrderId)
                    .Select(x => new
                    {
                        order_id = x.OrderId,
                        order_no = x.OrderNo,
                        order_date = x.OrderDate,
                        amount = x.Amount,
                        commission_amount = x.CommissionAmount,
                        role = x.Role,
                        share_amount = x.ShareAmount
                    })
            };

            var snapshotJson = JsonSerializer.Serialize(snapshot);

            if (existingPayments.TryGetValue(group.Key.UserId, out var existingPayment))
            {
                if (!request.OverwriteExisting)
                {
                    continue;
                }

                existingPayment.ExpectedAmount = expectedAmount;
                existingPayment.ActualAmount = null;
                existingPayment.PaymentStatus = "pending";
                existingPayment.SnapshotJson = snapshotJson;
                existingPayment.PaidAt = null;
                existingPayment.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Payments.Add(new Payment
                {
                    UserId = group.Key.UserId,
                    PayMonth = request.PayMonth,
                    ExpectedAmount = expectedAmount,
                    PaymentStatus = "pending",
                    SnapshotJson = snapshotJson
                });
            }
        }

        await _db.SaveChangesAsync();

        var payments = await _db.Payments.AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.PayMonth == request.PayMonth && userIds.Contains(x.UserId))
            .OrderBy(x => x.User.Nickname)
            .Select(x => ToDto(x))
            .ToListAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "generate_monthly",
            targetType: "payments",
            after: new
            {
                request.PayMonth,
                request.OverwriteExisting,
                PaymentCount = payments.Count,
                TotalExpectedAmount = payments.Sum(x => x.ExpectedAmount)
            }));
        await _db.SaveChangesAsync();

        return Ok(payments);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePayment(int id, UpdatePaymentRequestDto request)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.Id == id);
        if (payment is null)
        {
            return NotFound();
        }

        if (!DomainValues.IsPaymentStatus(request.PaymentStatus))
        {
            return ApiErrors.BadRequest("invalid_payment_status", "PaymentStatus must be pending, paid, or cancelled.");
        }

        var before = new
        {
            payment.ExpectedAmount,
            payment.ActualAmount,
            payment.PaymentStatus,
            payment.SnapshotJson,
            payment.PaidAt,
            payment.Note
        };

        payment.ExpectedAmount = request.ExpectedAmount;
        payment.ActualAmount = request.ActualAmount;
        payment.PaymentStatus = request.PaymentStatus;
        payment.SnapshotJson = request.SnapshotJson;
        payment.PaidAt = request.PaidAt;
        payment.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        payment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update",
            targetType: "payments",
            targetId: payment.Id,
            targetUuid: payment.Uuid,
            before: before,
            after: new
            {
                payment.ExpectedAmount,
                payment.ActualAmount,
                payment.PaymentStatus,
                payment.SnapshotJson,
                payment.PaidAt,
                payment.Note
            }));
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/mark-paid")]
    public async Task<IActionResult> MarkPaid(int id, MarkPaymentPaidRequestDto request)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.Id == id);
        if (payment is null)
        {
            return NotFound();
        }

        var before = new
        {
            payment.ActualAmount,
            payment.PaymentStatus,
            payment.PaidAt,
            payment.Note
        };

        payment.ActualAmount = request.ActualAmount ?? payment.ExpectedAmount;
        payment.PaymentStatus = "paid";
        payment.PaidAt = request.PaidAt ?? DateTime.UtcNow;
        payment.Note = string.IsNullOrWhiteSpace(request.Note) ? payment.Note : request.Note.Trim();
        payment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "mark_paid",
            targetType: "payments",
            targetId: payment.Id,
            targetUuid: payment.Uuid,
            before: before,
            after: new
            {
                payment.ActualAmount,
                payment.PaymentStatus,
                payment.PaidAt,
                payment.Note
            }));
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static bool TryParsePayMonth(string payMonth, out DateOnly monthStart)
    {
        monthStart = default;

        if (payMonth.Length != 7 || payMonth[4] != '-')
        {
            return false;
        }

        return int.TryParse(payMonth[..4], out var year)
            && int.TryParse(payMonth[5..], out var month)
            && month is >= 1 and <= 12
            && TryCreateDate(year, month, out monthStart);
    }

    private static bool TryCreateDate(int year, int month, out DateOnly value)
    {
        try
        {
            value = new DateOnly(year, month, 1);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = default;
            return false;
        }
    }

    private static PaymentDto ToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            Uuid = payment.Uuid,
            UserId = payment.UserId,
            Nickname = payment.User.Nickname,
            PayMonth = payment.PayMonth,
            ExpectedAmount = payment.ExpectedAmount,
            ActualAmount = payment.ActualAmount,
            PaymentStatus = payment.PaymentStatus,
            SnapshotJson = payment.SnapshotJson,
            PaidAt = payment.PaidAt,
            Note = payment.Note,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}
