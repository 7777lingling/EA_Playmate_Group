using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
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
            return BadRequest("Amount must be greater than zero.");
        }

        if (request.Members.Count == 0)
        {
            return BadRequest("At least one order member is required.");
        }

        var commissionAmount = request.CommissionAmount
            ?? decimal.Round(request.Amount * request.CommissionRate, 2, MidpointRounding.AwayFromZero);
        var distributableAmount = request.Amount - commissionAmount;
        var shareTotal = request.Members.Sum(x => x.ShareAmount);

        if (commissionAmount < 0 || commissionAmount > request.Amount)
        {
            return BadRequest("Commission amount must be between zero and amount.");
        }

        if (shareTotal != distributableAmount)
        {
            return BadRequest($"Share total must equal amount - commission amount. Expected {distributableAmount}, got {shareTotal}.");
        }

        var userIds = request.Members.Select(x => x.UserId).Distinct().ToList();
        var validUserCount = await _db.Users.CountAsync(x => userIds.Contains(x.Id) && x.IsActive);
        if (validUserCount != userIds.Count)
        {
            return BadRequest("One or more order members do not exist or are inactive.");
        }

        if (request.OwnerUserId.HasValue)
        {
            var ownerExists = await _db.Users.AnyAsync(x => x.Id == request.OwnerUserId.Value && x.IsActive);
            if (!ownerExists)
            {
                return BadRequest("Owner user does not exist or is inactive.");
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

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToDto(savedOrder));
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
}
