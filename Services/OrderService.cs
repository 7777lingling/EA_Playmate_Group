using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class OrderService
{
    private readonly EAPlaymateGroupDbContext _db;

    public OrderService(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderRequestDto request)
    {
        var validationResult = await ValidateCreateOrderAsync(request);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<OrderDto>(validationResult);
        }

        var commissionAmount = request.CommissionAmount
            ?? decimal.Round(request.Amount * request.CommissionRate, 2, MidpointRounding.AwayFromZero);

        var order = new Order
        {
            OrderNo = string.IsNullOrWhiteSpace(request.OrderNo) ? null : request.OrderNo.Trim(),
            OrderDate = request.OrderDate,
            OwnerUserId = request.OwnerUserId,
            Amount = request.Amount,
            CommissionRate = request.CommissionRate,
            CommissionAmount = commissionAmount,
            Status = request.Status,
            CustomerPaymentStatus = request.CustomerPaymentStatus,
            Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
            Members = request.Members.Select(x => new OrderMember
            {
                UserId = x.UserId,
                Role = x.Role,
                ShareAmount = x.ShareAmount
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var savedOrder = await GetOrderWithRelations(order.Id).FirstAsync();
        var dto = OrderMapper.ToDto(savedOrder);

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "create",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            after: dto));
        await _db.SaveChangesAsync();

        return ServiceResult<OrderDto>.Success(dto);
    }

    public async Task<ServiceResult> UpdateOrderAsync(int id, UpdateOrderRequestDto request)
    {
        var order = await _db.Orders
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
        {
            return ServiceResult.Missing();
        }

        var validationResult = await ValidateUpdateOrderAsync(request);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var before = OrderMapper.ToDto(order);

        order.OrderNo = string.IsNullOrWhiteSpace(request.OrderNo) ? null : request.OrderNo.Trim();
        order.OrderDate = request.OrderDate;
        order.OwnerUserId = request.OwnerUserId;
        order.Amount = request.Amount;
        order.CommissionRate = request.CommissionRate;
        order.CommissionAmount = request.CommissionAmount;
        order.Status = request.Status;
        order.CustomerPaymentStatus = request.CustomerPaymentStatus;
        order.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderMembers.RemoveRange(order.Members);
        order.Members = request.Members.Select(x => new OrderMember
        {
            OrderId = order.Id,
            UserId = x.UserId,
            Role = x.Role,
            ShareAmount = x.ShareAmount
        }).ToList();

        await _db.SaveChangesAsync();

        var savedOrder = await GetOrderWithRelations(order.Id).FirstAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: OrderMapper.ToDto(savedOrder)));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelOrderAsync(int id, UpdateOrderStatusRequestDto request)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return ServiceResult.Missing();
        }

        var before = new
        {
            order.Status,
            order.Remark
        };

        order.Status = "cancelled";
        order.Remark = string.IsNullOrWhiteSpace(request.Remark) ? order.Remark : request.Remark.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "cancel",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: new
            {
                order.Status,
                order.Remark
            }));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateStatusAsync(int id, UpdateOrderStatusRequestDto request)
    {
        if (!DomainValues.IsOrderStatus(request.Status))
        {
            return ServiceResult.Failure("invalid_order_status", "Invalid order status.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return ServiceResult.Missing();
        }

        var before = new
        {
            order.Status,
            order.Remark
        };

        order.Status = request.Status;
        order.Remark = string.IsNullOrWhiteSpace(request.Remark) ? order.Remark : request.Remark.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update_status",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: new
            {
                order.Status,
                order.Remark
            }));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateCustomerPaymentStatusAsync(int id, UpdateCustomerPaymentStatusRequestDto request)
    {
        if (!DomainValues.IsCustomerPaymentStatus(request.CustomerPaymentStatus))
        {
            return ServiceResult.Failure("invalid_customer_payment_status", "Invalid customer payment status.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return ServiceResult.Missing();
        }

        var before = new
        {
            order.CustomerPaymentStatus
        };

        order.CustomerPaymentStatus = request.CustomerPaymentStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update_customer_payment_status",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: new
            {
                order.CustomerPaymentStatus
            }));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private async Task<ServiceResult> ValidateCreateOrderAsync(CreateOrderRequestDto request)
    {
        var commissionAmount = request.CommissionAmount
            ?? decimal.Round(request.Amount * request.CommissionRate, 2, MidpointRounding.AwayFromZero);

        return await ValidateOrderAsync(
            request.Amount,
            commissionAmount,
            request.Status,
            request.CustomerPaymentStatus,
            request.OwnerUserId,
            request.Members);
    }

    private async Task<ServiceResult> ValidateUpdateOrderAsync(UpdateOrderRequestDto request)
    {
        return await ValidateOrderAsync(
            request.Amount,
            request.CommissionAmount,
            request.Status,
            request.CustomerPaymentStatus,
            request.OwnerUserId,
            request.Members);
    }

    private async Task<ServiceResult> ValidateOrderAsync(
        decimal amount,
        decimal commissionAmount,
        string status,
        string customerPaymentStatus,
        int? ownerUserId,
        IReadOnlyCollection<CreateOrderMemberRequestDto> members)
    {
        if (amount <= 0)
        {
            return ServiceResult.Failure("invalid_amount", "Amount must be greater than zero.");
        }

        if (members.Count == 0)
        {
            return ServiceResult.Failure("missing_order_members", "At least one order member is required.");
        }

        var valueErrors = ValidateOrderValues(status, customerPaymentStatus, members.Select(x => x.Role));
        if (valueErrors.Count > 0)
        {
            return ServiceResult.Validation(valueErrors);
        }

        if (commissionAmount < 0 || commissionAmount > amount)
        {
            return ServiceResult.Failure("invalid_commission_amount", "Commission amount must be between zero and amount.");
        }

        var distributableAmount = amount - commissionAmount;
        var shareTotal = members.Sum(x => x.ShareAmount);
        if (shareTotal != distributableAmount)
        {
            return ServiceResult.Failure("invalid_share_total", $"Share total must equal amount - commission amount. Expected {distributableAmount}, got {shareTotal}.");
        }

        var userIds = members.Select(x => x.UserId).Distinct().ToList();
        var validUserCount = await _db.Users.CountAsync(x => userIds.Contains(x.Id) && x.IsActive);
        if (validUserCount != userIds.Count)
        {
            return ServiceResult.Failure("invalid_order_member", "One or more order members do not exist or are inactive.");
        }

        if (ownerUserId.HasValue)
        {
            var ownerExists = await _db.Users.AnyAsync(x => x.Id == ownerUserId.Value && x.IsActive);
            if (!ownerExists)
            {
                return ServiceResult.Failure("invalid_owner_user", "Owner user does not exist or is inactive.");
            }
        }

        return ServiceResult.Success();
    }

    private IQueryable<Order> GetOrderWithRelations(int orderId)
    {
        return _db.Orders.AsNoTracking()
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Where(x => x.Id == orderId);
    }

    private static Dictionary<string, string[]> ValidateOrderValues(
        string status,
        string customerPaymentStatus,
        IEnumerable<string> memberRoles)
    {
        var errors = new Dictionary<string, string[]>();

        if (!DomainValues.IsOrderStatus(status))
        {
            errors["status"] = ["Status must be draft, completed, cancelled, or disputed."];
        }

        if (!DomainValues.IsCustomerPaymentStatus(customerPaymentStatus))
        {
            errors["customerPaymentStatus"] = ["CustomerPaymentStatus must be unpaid, partial, paid, or refunded."];
        }

        var invalidRoles = memberRoles
            .Where(x => !DomainValues.IsOrderMemberRole(x))
            .Distinct()
            .ToList();
        if (invalidRoles.Count > 0)
        {
            errors["members.role"] = [$"Invalid role: {string.Join(", ", invalidRoles)}."];
        }

        return errors;
    }

    private static ServiceResult<T> ToGenericResult<T>(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            return ServiceResult<T>.Validation(result.ValidationErrors);
        }

        if (result.NotFound)
        {
            return ServiceResult<T>.Missing();
        }

        return ServiceResult<T>.Failure(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }
}
