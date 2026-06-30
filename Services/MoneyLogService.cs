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
        "monthly_settlement", "manual_adjustment", "adjustment"
    ];

    private readonly EAPlaymateGroupDbContext _db;
    private readonly FileAttachmentService _fileAttachmentService;

    public MoneyLogService(EAPlaymateGroupDbContext db, FileAttachmentService fileAttachmentService)
    {
        _db = db;
        _fileAttachmentService = fileAttachmentService;
    }

    public async Task<ServiceResult<MoneyLogDto>> AddManualAsync(CreateMoneyLogRequestDto request)
    {
        if (!Types.Contains(request.Type))
        {
            return ServiceResult<MoneyLogDto>.Failure("invalid_money_type", "Unsupported money log type.");
        }

        if (request.Amount == 0)
        {
            return ServiceResult<MoneyLogDto>.Failure("invalid_amount", "Amount cannot be zero.");
        }

        if (RequiresAttachment(request.Type))
        {
            return ServiceResult<MoneyLogDto>.Failure(
                "attachment_required",
                "Attachment is required for manual deposit, refund, and adjustment money logs.");
        }

        var amount = NormalizeAmount(request.Type, request.Amount);
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

    public async Task<ServiceResult<MoneyLogDto>> AddManualWithAttachmentsAsync(
        CreateMoneyLogRequestDto request,
        IReadOnlyCollection<IFormFile> attachments)
    {
        if (!Types.Contains(request.Type))
        {
            return ServiceResult<MoneyLogDto>.Failure("invalid_money_type", "Unsupported money log type.");
        }

        if (request.Amount == 0)
        {
            return ServiceResult<MoneyLogDto>.Failure("invalid_amount", "Amount cannot be zero.");
        }

        if (RequiresAttachment(request.Type) && attachments.Count == 0)
        {
            return ServiceResult<MoneyLogDto>.Failure(
                "attachment_required",
                "Attachment is required for manual deposit, refund, and adjustment money logs.");
        }

        if (attachments.Count > 0)
        {
            var attachmentValidation = _fileAttachmentService.ValidateFiles(attachments);
            if (!attachmentValidation.Succeeded)
            {
                return ServiceResult<MoneyLogDto>.Failure(
                    attachmentValidation.ErrorCode ?? "invalid_attachment",
                    attachmentValidation.ErrorMessage ?? "Invalid attachment.");
            }
        }

        var log = await AddAsync(
            request.UserId,
            request.Type,
            NormalizeAmount(request.Type, request.Amount),
            sourceType: string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source.Trim(),
            note: request.Note);
        if (log is null)
        {
            return ServiceResult<MoneyLogDto>.Missing();
        }

        if (attachments.Count > 0)
        {
            var targetId = ToNullableInt(log.Id);
            if (!targetId.HasValue)
            {
                return ServiceResult<MoneyLogDto>.Failure("invalid_target", "Money log id is too large for attachments.");
            }

            var savedAttachments = await _fileAttachmentService.UploadManyAsync(
                "money_logs",
                targetId.Value,
                attachments,
                "money_proof",
                request.Note);

            _db.AuditLogs.Add(AuditLogWriter.Create(
                "bind_attachments",
                "money_logs",
                targetId,
                log.SourceUuid,
                after: new
                {
                    moneyLogId = log.Id,
                    attachmentIds = savedAttachments.Select(x => x.Id).ToList(),
                    attachmentCount = savedAttachments.Count
                },
                userId: log.UserId,
                correlationId: log.CorrelationId));
            await _db.SaveChangesAsync();
        }

        return ServiceResult<MoneyLogDto>.Success(ToDto(log));
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
                "Reversal money logs cannot be reversed again.");
        }

        var alreadyReversed = await _db.MoneyLogs.AnyAsync(x => x.ReversedMoneyLogId == id);
        if (alreadyReversed)
        {
            return ServiceResult<MoneyLogDto>.Failure(
                "money_log_already_reversed",
                "This money log has already been reversed.");
        }

        var note = string.IsNullOrWhiteSpace(request.Note)
            ? $"Reverse money log #{original.Id}"
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
                balanceBefore = previousBalance,
                balanceAfter,
                status = "completed",
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
            BalanceBefore = previousBalance,
            BalanceAfter = balanceAfter,
            Status = "completed",
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
        LoginUserId = log.LoginUserId,
        OperatorDisplayName = log.LoginUser?.DisplayName,
        OperatorLoginAccount = log.LoginUser?.LoginAccount,
        AuditLogId = log.AuditLogId,
        ReversedMoneyLogId = log.ReversedMoneyLogId,
        Type = log.Type,
        Amount = log.Amount,
        BalanceBefore = log.BalanceBefore,
        BalanceAfter = log.BalanceAfter,
        Status = log.Status,
        SourceType = log.SourceType,
        SourceId = log.SourceId,
        SourceUuid = log.SourceUuid,
        Note = log.Note,
        IsReversal = log.IsReversal,
        CorrelationId = log.CorrelationId,
        CreatedAt = log.CreatedAt
    };

    private static decimal NormalizeAmount(string type, decimal amount) =>
        type switch
        {
            "deduction" or "monthly_settlement" => -Math.Abs(amount),
            "deposit" or "refund" or "gift_income" => Math.Abs(amount),
            _ => amount
        };

    private static bool RequiresAttachment(string type) =>
        type is "deposit" or "refund" or "manual_adjustment" or "adjustment";

    private static int? ToNullableInt(long value) =>
        value <= int.MaxValue ? (int)value : null;
}
