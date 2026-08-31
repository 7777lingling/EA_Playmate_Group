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
            OrderType = order.OrderType,
            PricingCategory = order.PricingCategory,
            OrderDate = order.OrderDate,
            OwnerUserId = order.OwnerUserId,
            OwnerNickname = order.OwnerUser?.Nickname,
            Amount = order.Amount,
            ServiceQuantity = order.ServiceQuantity,
            BaseAmount = order.BaseAmount,
            DesignatedFee = order.DesignatedFee,
            FriendFee = order.FriendFee,
            ReplacementFee = order.ReplacementFee,
            NightFee = order.NightFee,
            OtherFee = order.OtherFee,
            DiscountAmount = order.DiscountAmount,
            FinalAmount = order.FinalAmount == 0m ? order.Amount : order.FinalAmount,
            ActivityId = order.ActivityId,
            ActivityNameSnapshot = order.ActivityNameSnapshot,
            ActivityDiscountType = order.ActivityDiscountType,
            ActivityDiscountValue = order.ActivityDiscountValue,
            ActivityIncludeFees = order.ActivityIncludeFees,
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
