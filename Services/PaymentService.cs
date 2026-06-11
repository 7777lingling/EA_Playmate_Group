using System.Text.Json;
using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class PaymentService
{
    private readonly EAPlaymateGroupDbContext _db;

    public PaymentService(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<List<PaymentDto>>> GenerateMonthlyPaymentsAsync(GenerateMonthlyPaymentsRequestDto request)
    {
        if (!TryParsePayMonth(request.PayMonth, out var monthStart))
        {
            return ServiceResult<List<PaymentDto>>.Failure("invalid_pay_month", "PayMonth must use yyyy-MM format.");
        }

        var nextMonth = monthStart.AddMonths(1);

        var orderRows = await _db.OrderMembers.AsNoTracking()
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

        var giftRows = await _db.GiftRecords.AsNoTracking()
            .Where(x => x.Status == "completed"
                && x.GiftDate >= monthStart
                && x.GiftDate < nextMonth)
            .Select(x => new
            {
                UserId = x.RecipientUserId,
                Nickname = x.RecipientUser!.Nickname,
                GiftRecordId = x.Id,
                x.GiftDate,
                x.GiftName,
                x.Amount,
                x.Quantity,
                TotalAmount = x.Amount * x.Quantity,
                x.BossUserId,
                BossNickname = x.BossUser!.Nickname,
                x.CustomerPaymentStatus
            })
            .ToListAsync();

        var userIds = orderRows.Select(x => x.UserId)
            .Concat(giftRows.Select(x => x.UserId))
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return ServiceResult<List<PaymentDto>>.Success([]);
        }

        var nicknames = orderRows.Select(x => new { x.UserId, x.Nickname })
            .Concat(giftRows.Select(x => new { x.UserId, x.Nickname }))
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.First().Nickname);
        var orderGroups = orderRows.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.ToList());
        var giftGroups = giftRows.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.ToList());

        var existingPayments = await _db.Payments
            .Where(x => x.PayMonth == request.PayMonth && userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);

        foreach (var userId in userIds)
        {
            orderGroups.TryGetValue(userId, out var userOrders);
            giftGroups.TryGetValue(userId, out var userGifts);

            userOrders ??= [];
            userGifts ??= [];

            var orderAmount = userOrders.Sum(x => x.ShareAmount);
            var giftAmount = userGifts.Sum(x => x.TotalAmount);
            var expectedAmount = orderAmount + giftAmount;
            var snapshot = new
            {
                pay_month = request.PayMonth,
                user_id = userId,
                nickname = nicknames[userId],
                expected_amount = expectedAmount,
                order_amount = orderAmount,
                gift_amount = giftAmount,
                orders = userOrders
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
                    }),
                gifts = userGifts
                    .OrderBy(x => x.GiftDate)
                    .ThenBy(x => x.GiftRecordId)
                    .Select(x => new
                    {
                        gift_record_id = x.GiftRecordId,
                        gift_date = x.GiftDate,
                        gift_name = x.GiftName,
                        boss_user_id = x.BossUserId,
                        boss_nickname = x.BossNickname,
                        amount = x.Amount,
                        quantity = x.Quantity,
                        total_amount = x.TotalAmount,
                        customer_payment_status = x.CustomerPaymentStatus
                    })
            };

            var snapshotJson = JsonSerializer.Serialize(snapshot);

            if (existingPayments.TryGetValue(userId, out var existingPayment))
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
                    UserId = userId,
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
            .Select(x => PaymentMapper.ToDto(x))
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

        return ServiceResult<List<PaymentDto>>.Success(payments);
    }

    public async Task<ServiceResult> UpdatePaymentAsync(int id, UpdatePaymentRequestDto request)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.Id == id);
        if (payment is null)
        {
            return ServiceResult.Missing();
        }

        if (!DomainValues.IsPaymentStatus(request.PaymentStatus))
        {
            return ServiceResult.Failure("invalid_payment_status", "PaymentStatus must be pending, paid, or cancelled.");
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

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> MarkPaidAsync(int id, MarkPaymentPaidRequestDto request)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.Id == id);
        if (payment is null)
        {
            return ServiceResult.Missing();
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

        return ServiceResult.Success();
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
}
