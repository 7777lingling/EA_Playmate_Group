using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class GiftRecordService
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly MoneyLogService _moneyLogService;

    public GiftRecordService(EAPlaymateGroupDbContext db, MoneyLogService moneyLogService)
    {
        _db = db;
        _moneyLogService = moneyLogService;
    }

    public async Task<ServiceResult<GiftRecordDto>> CreateAsync(CreateGiftRecordRequestDto request)
    {
        var validation = await ValidateAsync(request.GiftDate, request.BossUserId, request.RecipientUserId, request.ServiceItemId, request.GiftName, request.Amount, request.Quantity, request.CustomerPaymentStatus, request.Status);
        if (!validation.Succeeded)
        {
            return ToGenericResult<GiftRecordDto>(validation);
        }

        var giftName = await ResolveGiftNameAsync(request.ServiceItemId, request.GiftName);
        var record = new GiftRecord
        {
            GiftDate = request.GiftDate,
            BossUserId = request.BossUserId,
            RecipientUserId = request.RecipientUserId,
            ServiceItemId = request.ServiceItemId,
            GiftName = giftName,
            Amount = request.Amount,
            Quantity = request.Quantity,
            CustomerPaymentStatus = request.CustomerPaymentStatus,
            Status = request.Status,
            Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim()
        };

        _db.GiftRecords.Add(record);
        await _db.SaveChangesAsync();

        var saved = await GetWithRelations(record.Id).FirstAsync();
        var dto = GiftRecordMapper.ToDto(saved);
        var audit = AuditLogWriter.Create(
            action: "create",
            targetType: "gift_records",
            targetId: record.Id,
            targetUuid: record.Uuid,
            after: dto);
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();

        record.CreatedAuditLogId = audit.Id;
        await _db.SaveChangesAsync();

        if (record.Status == "completed")
        {
            await _moneyLogService.AddAsync(
                record.RecipientUserId, "gift_income", record.Amount * record.Quantity,
                "gift_records", record.Id, record.Uuid, record.GiftName);
        }

        return ServiceResult<GiftRecordDto>.Success(dto);
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpdateGiftRecordRequestDto request)
    {
        var record = await GetWithRelations(id).FirstOrDefaultAsync();
        if (record is null)
        {
            return ServiceResult.Missing();
        }

        var validation = await ValidateAsync(request.GiftDate, request.BossUserId, request.RecipientUserId, request.ServiceItemId, request.GiftName, request.Amount, request.Quantity, request.CustomerPaymentStatus, request.Status);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var before = GiftRecordMapper.ToDto(record);
        record.GiftDate = request.GiftDate;
        record.BossUserId = request.BossUserId;
        record.RecipientUserId = request.RecipientUserId;
        record.ServiceItemId = request.ServiceItemId;
        record.GiftName = await ResolveGiftNameAsync(request.ServiceItemId, request.GiftName);
        record.Amount = request.Amount;
        record.Quantity = request.Quantity;
        record.CustomerPaymentStatus = request.CustomerPaymentStatus;
        record.Status = request.Status;
        record.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var saved = await GetWithRelations(id).FirstAsync();
        var oldAmount = before.Status == "completed" ? before.Amount * before.Quantity : 0m;
        var newAmount = saved.Status == "completed" ? saved.Amount * saved.Quantity : 0m;
        if (before.RecipientUserId == saved.RecipientUserId)
        {
            var delta = newAmount - oldAmount;
            if (delta != 0)
            {
                await _moneyLogService.AddAsync(saved.RecipientUserId, "gift_income", delta,
                    "gift_records", saved.Id, saved.Uuid, $"修改：{saved.GiftName}");
            }
        }
        else
        {
            if (oldAmount != 0)
            {
                await _moneyLogService.AddAsync(before.RecipientUserId, "gift_income", -oldAmount,
                    "gift_records", saved.Id, saved.Uuid, $"移轉：{before.GiftName}");
            }
            if (newAmount != 0)
            {
                await _moneyLogService.AddAsync(saved.RecipientUserId, "gift_income", newAmount,
                    "gift_records", saved.Id, saved.Uuid, $"移轉：{saved.GiftName}");
            }
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelAsync(int id)
    {
        var record = await _db.GiftRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (record is null)
        {
            return ServiceResult.Missing();
        }

        var before = new { record.Status };
        record.Status = "cancelled";
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var audit = AuditLogWriter.Create(
            action: "cancel",
            targetType: "gift_records",
            targetId: record.Id,
            targetUuid: record.Uuid,
            before: before,
            after: new { record.Status });
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();

        record.CancelledAuditLogId = audit.Id;
        await _db.SaveChangesAsync();

        if (before.Status == "completed")
        {
            await _moneyLogService.AddAsync(record.RecipientUserId, "gift_income",
                -(record.Amount * record.Quantity), "gift_records", record.Id, record.Uuid, "取消禮物收入");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var record = await GetWithRelations(id).FirstOrDefaultAsync();
        if (record is null)
        {
            return ServiceResult.Missing();
        }

        var before = GiftRecordMapper.ToDto(record);
        _db.GiftRecords.Remove(record);
        await _db.SaveChangesAsync();

        if (before.Status == "completed")
        {
            await _moneyLogService.AddAsync(before.RecipientUserId, "gift_income",
                -(before.Amount * before.Quantity), "gift_records", before.Id, before.Uuid, "刪除禮物收入");
        }

        return ServiceResult.Success();
    }

    private IQueryable<GiftRecord> GetWithRelations(int id)
    {
        return _db.GiftRecords
            .Include(x => x.BossUser)
            .Include(x => x.RecipientUser)
            .Include(x => x.ServiceItem)
            .Where(x => x.Id == id);
    }

    private async Task<ServiceResult> ValidateAsync(
        DateOnly giftDate,
        int bossUserId,
        int recipientUserId,
        int? serviceItemId,
        string? giftName,
        decimal amount,
        decimal quantity,
        string customerPaymentStatus,
        string status)
    {
        var errors = new Dictionary<string, string[]>();

        if (giftDate == default)
        {
            errors["giftDate"] = ["請選擇日期。"];
        }

        if (amount <= 0)
        {
            errors["amount"] = ["金額必須大於 0。"];
        }

        if (quantity <= 0)
        {
            errors["quantity"] = ["數量必須大於 0。"];
        }

        if (!DomainValues.IsCustomerPaymentStatus(customerPaymentStatus))
        {
            errors["customerPaymentStatus"] = ["收款狀態不正確。"];
        }

        if (!DomainValues.IsGiftRecordStatus(status))
        {
            errors["status"] = ["紀錄狀態不正確。"];
        }

        if (!serviceItemId.HasValue && string.IsNullOrWhiteSpace(giftName))
        {
            errors["giftName"] = ["請選擇禮物項目或輸入打賞名稱。"];
        }

        var bossExists = await _db.Users.AnyAsync(x => x.Id == bossUserId && x.IsBoss && x.IsActive);
        if (!bossExists)
        {
            errors["bossUserId"] = ["請選擇有效老闆。"];
        }

        var recipientExists = await _db.Users.AnyAsync(x => x.Id == recipientUserId && x.IsPlayer && x.IsActive);
        if (!recipientExists)
        {
            errors["recipientUserId"] = ["請選擇有效收禮團員。"];
        }

        if (serviceItemId.HasValue)
        {
            var giftItemExists = await _db.ServiceItems.AnyAsync(x => x.Id == serviceItemId && x.Category == "gift" && x.IsActive);
            if (!giftItemExists)
            {
                errors["serviceItemId"] = ["請選擇有效禮物項目。"];
            }
        }

        return errors.Count == 0 ? ServiceResult.Success() : ServiceResult.Validation(errors);
    }

    private async Task<string> ResolveGiftNameAsync(int? serviceItemId, string? giftName)
    {
        if (serviceItemId.HasValue)
        {
            var itemName = await _db.ServiceItems
                .Where(x => x.Id == serviceItemId.Value)
                .Select(x => x.Name)
                .FirstAsync();
            return itemName;
        }

        return giftName!.Trim();
    }

    private static ServiceResult<T> ToGenericResult<T>(ServiceResult result)
    {
        return new ServiceResult<T>
        {
            NotFound = result.NotFound,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            ValidationErrors = result.ValidationErrors
        };
    }
}
