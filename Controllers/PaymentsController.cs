using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly PaymentService _paymentService;

    public PaymentsController(EAPlaymateGroupDbContext db, PaymentService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments(
        [FromQuery] string? payMonth,
        [FromQuery] string? status)
    {
        var query = _db.Payments.AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(payMonth))
        {
            query = query.Where(x => x.PayMonth == payMonth);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.PaymentStatus == status);
        }

        var payments = await query
            .OrderByDescending(x => x.PayMonth)
            .ThenBy(x => x.User.Nickname)
            .Select(x => PaymentMapper.ToDto(x))
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(int id)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        return payment is null ? NotFound() : Ok(PaymentMapper.ToDto(payment));
    }

    [HttpPost("generate-monthly")]
    public async Task<ActionResult<List<PaymentDto>>> GenerateMonthlyPayments(GenerateMonthlyPaymentsRequestDto request)
    {
        var result = await _paymentService.GenerateMonthlyPaymentsAsync(request);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPost("{id:int}/mark-paid")]
    public async Task<IActionResult> MarkPaid(int id, MarkPaymentPaidRequestDto request)
    {
        var result = await _paymentService.MarkPaidAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
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
