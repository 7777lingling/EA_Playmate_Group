using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GiftRecordsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly GiftRecordService _giftRecordService;

    public GiftRecordsController(EAPlaymateGroupDbContext db, GiftRecordService giftRecordService)
    {
        _db = db;
        _giftRecordService = giftRecordService;
    }

    [HttpGet]
    public async Task<ActionResult<List<GiftRecordDto>>> GetGiftRecords(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? status)
    {
        var query = _db.GiftRecords.AsNoTracking()
            .Include(x => x.BossUser)
            .Include(x => x.RecipientUser)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(x => x.GiftDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.GiftDate <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var records = await query
            .OrderByDescending(x => x.GiftDate)
            .ThenByDescending(x => x.Id)
            .Take(200)
            .ToListAsync();

        return Ok(records.Select(GiftRecordMapper.ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GiftRecordDto>> GetGiftRecord(int id)
    {
        var record = await _db.GiftRecords.AsNoTracking()
            .Include(x => x.BossUser)
            .Include(x => x.RecipientUser)
            .Include(x => x.ServiceItem)
            .FirstOrDefaultAsync(x => x.Id == id);

        return record is null ? NotFound() : Ok(GiftRecordMapper.ToDto(record));
    }

    [HttpPost]
    public async Task<ActionResult<GiftRecordDto>> CreateGiftRecord(CreateGiftRecordRequestDto request)
    {
        var result = await _giftRecordService.CreateAsync(request);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetGiftRecord), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateGiftRecord(int id, UpdateGiftRecordRequestDto request)
    {
        var result = await _giftRecordService.UpdateAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelGiftRecord(int id)
    {
        var result = await _giftRecordService.CancelAsync(id);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteGiftRecord(int id)
    {
        var result = await _giftRecordService.DeleteAsync(id);
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
