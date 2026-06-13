using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly OrderService _orderService;

    public OrdersController(EAPlaymateGroupDbContext db, OrderService orderService)
    {
        _db = db;
        _orderService = orderService;
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

        return order is null ? NotFound() : Ok(OrderMapper.ToDto(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequestDto request)
    {
        var result = await _orderService.CreateOrderAsync(request);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetOrder), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOrder(int id, UpdateOrderRequestDto request)
    {
        var result = await _orderService.UpdateOrderAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, UpdateOrderStatusRequestDto request)
    {
        var result = await _orderService.CancelOrderAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequestDto request)
    {
        var result = await _orderService.UpdateStatusAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/customer-payment-status")]
    public async Task<IActionResult> UpdateCustomerPaymentStatus(int id, UpdateCustomerPaymentStatusRequestDto request)
    {
        var result = await _orderService.UpdateCustomerPaymentStatusAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _db.Orders.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        var before = OrderMapper.ToDto(order);
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create("delete", "orders", id, order.Uuid, before: before));
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private ActionResult ToActionResult(ServiceResult result)
    {
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.ValidationErrors is not null)
        {
            return ApiErrors.Validation(result.ValidationErrors);
        }

        return ApiErrors.BadRequest(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }

    private ActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        return ToActionResult(new ServiceResult
        {
            NotFound = result.NotFound,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            ValidationErrors = result.ValidationErrors
        });
    }
}
