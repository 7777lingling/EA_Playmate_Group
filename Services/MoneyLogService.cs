using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class MoneyLogService
{
    private static readonly HashSet<string> Types =
    [
        "deposit", "deduction", "refund", "gift_income",
        "monthly_settlement", "manual_adjustment"
    ];

    private readonly EAPlaymateGroupDbContext _db;

    public MoneyLogService(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<MoneyLogDto>> AddManualAsync(CreateMoneyLogRequestDto request)
    {
        if (!Types.Contains(request.Type))
        {
            return ServiceResult<MoneyLogDto>.Failure("invalid_money_type", "不支援的金流類型。");
        }

        if (request.Amount == 0)
        {
            return ServiceResult<MoneyLogDto>.Failure("invalid_amount", "金額不可為 0。");
        }

        var amount = request.Type switch
        {
            "deduction" or "monthly_settlement" => -Math.Abs(request.Amount),
            "deposit" or "refund" or "gift_income" => Math.Abs(request.Amount),
            _ => request.Amount
        };

        var log = await AddAsync(
            request.UserId,
            request.Type,
            amount,
            sourceType: string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source.Trim(),
            note: request.Note);
        return log is null
            ? ServiceResult<MoneyLogDto>.Missing()
            : ServiceResult<MoneyLogDto>.Success(ToDto(log));
    }

    public async Task<ServiceResult<MoneyLogDto>> ReverseAsync(long id, ReverseMoneyLogRequestDto request)
    {
        var original = await _db.MoneyLogs
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (original is null)
        {
            return ServiceResult<MoneyLogDto>.Missing();
        }

        if (original.IsReversal)
        {
            return ServiceResult<MoneyLogDto>.Failure(
                "cannot_reverse_reversal",
                "沖正紀錄不可再次沖正。");
        }

        var alreadyReversed = await _db.MoneyLogs.AnyAsync(x => x.ReversedMoneyLogId == id);
        if (alreadyReversed)
        {
            return ServiceResult<MoneyLogDto>.Failure(
                "money_log_already_reversed",
                "此金流紀錄已沖正。");
        }

        var note = string.IsNullOrWhiteSpace(request.Note)
            ? $"沖正金流紀錄 #{original.Id}"
            : request.Note.Trim();

        var reversal = await AddAsync(
            original.UserId,
            original.Type,
            -original.Amount,
            sourceType: "money_logs",
            sourceId: ToNullableInt(original.Id),
            note: note,
            isReversal: true,
            reversedMoneyLogId: original.Id);

        return reversal is null
            ? ServiceResult<MoneyLogDto>.Missing()
            : ServiceResult<MoneyLogDto>.Success(ToDto(reversal));
    }

    public async Task<MoneyLog?> AddAsync(
        int userId,
        string type,
        decimal amount,
        string? sourceType = null,
        int? sourceId = null,
        Guid? sourceUuid = null,
        string? note = null,
        bool isReversal = false,
        long? reversedMoneyLogId = null,
        Guid? correlationId = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
        {
            return null;
        }

        correlationId ??= Guid.NewGuid();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var previousBalance = await _db.MoneyLogs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .Select(x => (decimal?)x.BalanceAfter)
            .FirstOrDefaultAsync() ?? 0m;

        var roundedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        var balanceAfter = decimal.Round(previousBalance + roundedAmount, 2, MidpointRounding.AwayFromZero);
        var audit = AuditLogWriter.Create(
            isReversal ? "reverse" : "create",
            "money_logs",
            after: new
            {
                userId,
                user.Nickname,
                type,
                amount = roundedAmount,
                balanceAfter,
                sourceType,
                sourceId,
                sourceUuid,
                note,
                isReversal,
                reversedMoneyLogId
            },
            userId: userId,
            correlationId: correlationId);
        _db.AuditLogs.Add(audit);

        var log = new MoneyLog
        {
            UserId = userId,
            User = user,
            AuditLog = audit,
            ReversedMoneyLogId = reversedMoneyLogId,
            Type = type,
            Amount = roundedAmount,
            BalanceAfter = balanceAfter,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceUuid = sourceUuid,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            IsReversal = isReversal,
            CorrelationId = correlationId.Value
        };
        _db.MoneyLogs.Add(log);
        await _db.SaveChangesAsync();

        audit.TargetId = ToNullableInt(log.Id);
        audit.TargetUuid = log.SourceUuid;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return log;
    }

    public static MoneyLogDto ToDto(MoneyLog log) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        MemberNickname = log.User?.Nickname ?? string.Empty,
        AuditLogId = log.AuditLogId,
        ReversedMoneyLogId = log.ReversedMoneyLogId,
        Type = log.Type,
        Amount = log.Amount,
        BalanceAfter = log.BalanceAfter,
        SourceType = log.SourceType,
        SourceId = log.SourceId,
        Note = log.Note,
        IsReversal = log.IsReversal,
        CorrelationId = log.CorrelationId,
        CreatedAt = log.CreatedAt
    };

    private static int? ToNullableInt(long value)
    {
        return value <= int.MaxValue ? (int)value : null;
    }
}
