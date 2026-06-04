using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class OrderMapper
{
    public static OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            Uuid = order.Uuid,
            OrderNo = order.OrderNo,
            OrderDate = order.OrderDate,
            OwnerUserId = order.OwnerUserId,
            OwnerNickname = order.OwnerUser?.Nickname,
            Amount = order.Amount,
            CommissionRate = order.CommissionRate,
            CommissionAmount = order.CommissionAmount,
            ShareTotalAmount = order.Members.Sum(x => x.ShareAmount),
            Status = order.Status,
            CustomerPaymentStatus = order.CustomerPaymentStatus,
            Remark = order.Remark,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Members = order.Members
                .OrderBy(x => x.Id)
                .Select(x => new OrderMemberDto
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    UserId = x.UserId,
                    Nickname = x.User.Nickname,
                    Role = x.Role,
                    ShareAmount = x.ShareAmount,
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };
    }
}
