using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class PaymentMapper
{
    public static PaymentDto ToDto(Payment payment)
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
