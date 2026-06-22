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

    public async Task<MoneyLog?> AddAsync(
        int userId,
        string type,
        decimal amount,
        string? sourceType = null,
        int? sourceId = null,
        Guid? sourceUuid = null,
        string? note = null)
    {
        if (!await _db.Users.AnyAsync(x => x.Id == userId))
        {
            return null;
        }

        var previousBalance = await _db.MoneyLogs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .Select(x => (decimal?)x.BalanceAfter)
            .FirstOrDefaultAsync() ?? 0m;

        var log = new MoneyLog
        {
            UserId = userId,
            Type = type,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            BalanceAfter = decimal.Round(previousBalance + amount, 2, MidpointRounding.AwayFromZero),
            SourceType = sourceType,
            SourceId = sourceId,
            SourceUuid = sourceUuid,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
        _db.MoneyLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public static MoneyLogDto ToDto(MoneyLog log) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        MemberNickname = log.User?.Nickname ?? string.Empty,
        Type = log.Type,
        Amount = log.Amount,
        BalanceAfter = log.BalanceAfter,
        SourceType = log.SourceType,
        SourceId = log.SourceId,
        Note = log.Note,
        CreatedAt = log.CreatedAt
    };
}
