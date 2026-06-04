using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public OrdersController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderListItemDto>>> GetOrders(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? status)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(x => x.OrderDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.OrderDate <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var orders = await query
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new OrderListItemDto
            {
                Id = x.Id,
                Uuid = x.Uuid,
                OrderNo = x.OrderNo,
                OrderDate = x.OrderDate,
                OwnerNickname = x.OwnerUser == null ? null : x.OwnerUser.Nickname,
                Amount = x.Amount,
                CommissionAmount = x.CommissionAmount,
                ShareTotalAmount = x.Members.Sum(m => m.ShareAmount),
                MemberCount = x.Members.Count,
                Status = x.Status,
                CustomerPaymentStatus = x.CustomerPaymentStatus
            })
            .Take(200)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        return order is null ? NotFound() : Ok(ToDto(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequestDto request)
    {
        if (request.Amount <= 0)
        {
            return ApiErrors.BadRequest("invalid_amount", "Amount must be greater than zero.");
        }

        if (request.Members.Count == 0)
        {
            return ApiErrors.BadRequest("missing_order_members", "At least one order member is required.");
        }

        var validationError = ValidateOrderValues(
            request.Status,
            request.CustomerPaymentStatus,
            request.Members.Select(x => x.Role));
        if (validationError is not null)
        {
            return validationError;
        }

        var commissionAmount = request.CommissionAmount
            ?? decimal.Round(request.Amount * request.CommissionRate, 2, MidpointRounding.AwayFromZero);
        var distributableAmount = request.Amount - commissionAmount;
        var shareTotal = request.Members.Sum(x => x.ShareAmount);

        if (commissionAmount < 0 || commissionAmount > request.Amount)
        {
            return ApiErrors.BadRequest("invalid_commission_amount", "Commission amount must be between zero and amount.");
        }

        if (shareTotal != distributableAmount)
        {
            return ApiErrors.BadRequest("invalid_share_total", $"Share total must equal amount - commission amount. Expected {distributableAmount}, got {shareTotal}.");
        }

        var userIds = request.Members.Select(x => x.UserId).Distinct().ToList();
        var validUserCount = await _db.Users.CountAsync(x => userIds.Contains(x.Id) && x.IsActive);
        if (validUserCount != userIds.Count)
        {
            return ApiErrors.BadRequest("invalid_order_member", "One or more order members do not exist or are inactive.");
        }

        if (request.OwnerUserId.HasValue)
        {
            var ownerExists = await _db.Users.AnyAsync(x => x.Id == request.OwnerUserId.Value && x.IsActive);
            if (!ownerExists)
            {
                return ApiErrors.BadRequest("invalid_owner_user", "Owner user does not exist or is inactive.");
            }
        }

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

        var savedOrder = await _db.Orders.AsNoTracking()
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstAsync(x => x.Id == order.Id);

        var dto = ToDto(savedOrder);

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "create",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            after: dto));
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOrder(int id, UpdateOrderRequestDto request)
    {
        var order = await _db.Orders
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (request.Amount <= 0)
        {
            return ApiErrors.BadRequest("invalid_amount", "Amount must be greater than zero.");
        }

        if (request.Members.Count == 0)
        {
            return ApiErrors.BadRequest("missing_order_members", "At least one order member is required.");
        }

        var validationError = ValidateOrderValues(
            request.Status,
            request.CustomerPaymentStatus,
            request.Members.Select(x => x.Role));
        if (validationError is not null)
        {
            return validationError;
        }

        var distributableAmount = request.Amount - request.CommissionAmount;
        var shareTotal = request.Members.Sum(x => x.ShareAmount);

        if (request.CommissionAmount < 0 || request.CommissionAmount > request.Amount)
        {
            return ApiErrors.BadRequest("invalid_commission_amount", "Commission amount must be between zero and amount.");
        }

        if (shareTotal != distributableAmount)
        {
            return ApiErrors.BadRequest("invalid_share_total", $"Share total must equal amount - commission amount. Expected {distributableAmount}, got {shareTotal}.");
        }

        var userIds = request.Members.Select(x => x.UserId).Distinct().ToList();
        var validUserCount = await _db.Users.CountAsync(x => userIds.Contains(x.Id) && x.IsActive);
        if (validUserCount != userIds.Count)
        {
            return ApiErrors.BadRequest("invalid_order_member", "One or more order members do not exist or are inactive.");
        }

        if (request.OwnerUserId.HasValue)
        {
            var ownerExists = await _db.Users.AnyAsync(x => x.Id == request.OwnerUserId.Value && x.IsActive);
            if (!ownerExists)
            {
                return ApiErrors.BadRequest("invalid_owner_user", "Owner user does not exist or is inactive.");
            }
        }

        var before = ToDto(order);

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

        var savedOrder = await _db.Orders.AsNoTracking()
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstAsync(x => x.Id == order.Id);

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: ToDto(savedOrder)));
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, UpdateOrderStatusRequestDto request)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
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

        return NoContent();
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequestDto request)
    {
        if (!DomainValues.IsOrderStatus(request.Status))
        {
            return ApiErrors.BadRequest("invalid_order_status", "Invalid order status.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
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

        return NoContent();
    }

    [HttpPost("{id:int}/customer-payment-status")]
    public async Task<IActionResult> UpdateCustomerPaymentStatus(int id, UpdateCustomerPaymentStatusRequestDto request)
    {
        if (!DomainValues.IsCustomerPaymentStatus(request.CustomerPaymentStatus))
        {
            return ApiErrors.BadRequest("invalid_customer_payment_status", "Invalid customer payment status.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
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

        return NoContent();
    }

    private static OrderDto ToDto(Order order)
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

    private static BadRequestObjectResult? ValidateOrderValues(
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

        return errors.Count == 0 ? null : ApiErrors.Validation(errors);
    }
}
