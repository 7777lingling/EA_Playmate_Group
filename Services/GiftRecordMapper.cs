using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class GiftRecordMapper
{
    public static GiftRecordDto ToDto(GiftRecord record)
    {
        return new GiftRecordDto
        {
            Id = record.Id,
            Uuid = record.Uuid,
            GiftDate = record.GiftDate,
            BossUserId = record.BossUserId,
            BossNickname = record.BossUser?.Nickname ?? string.Empty,
            RecipientUserId = record.RecipientUserId,
            RecipientNickname = record.RecipientUser?.Nickname ?? string.Empty,
            ServiceItemId = record.ServiceItemId,
            GiftName = record.GiftName,
            Amount = record.Amount,
            Quantity = record.Quantity,
            CustomerPaymentStatus = record.CustomerPaymentStatus,
            Status = record.Status,
            Remark = record.Remark,
            CreatedAt = record.CreatedAt
        };
    }
}
