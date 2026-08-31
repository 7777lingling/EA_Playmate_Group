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
[RequirePermission("Order.View")]
public sealed class ActivitiesController : ControllerBase
{
    private static readonly HashSet<string> ValidDiscountTypes = ["percent", "fixed_amount", "fixed_price"];
    private readonly EAPlaymateGroupDbContext _db;

    public ActivitiesController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ActivityDto>>> GetActivities([FromQuery] bool activeOnly = false)
    {
        var query = _db.Activities.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var rows = await query
            .OrderByDescending(x => x.StartsAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();
        return Ok(rows.Select(ActivityMapper.ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActivityDto>> GetActivity(int id)
    {
        var activity = await _db.Activities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return activity is null ? NotFound() : Ok(ActivityMapper.ToDto(activity));
    }

    [HttpPost]
    [RequirePermission("Order.Edit")]
    public async Task<ActionResult<ActivityDto>> CreateActivity(CreateActivityRequestDto request)
    {
        var validation = Validate(request.Name, request.StartsAt, request.EndsAt, request.DiscountType, request.DiscountValue);
        if (validation is not null)
        {
            return validation;
        }

        var activity = new Activity
        {
            Name = request.Name.Trim(),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            ApplicableCategories = NormalizeCategories(request.ApplicableCategories),
            IncludeFees = request.IncludeFees,
            IsActive = request.IsActive,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var dto = ActivityMapper.ToDto(activity);
        _db.AuditLogs.Add(AuditLogWriter.Create("create", "activities", activity.Id, activity.Uuid, after: dto));
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetActivity), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Order.Edit")]
    public async Task<IActionResult> UpdateActivity(int id, UpdateActivityRequestDto request)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(x => x.Id == id);
        if (activity is null)
        {
            return NotFound();
        }

        var validation = Validate(request.Name, request.StartsAt, request.EndsAt, request.DiscountType, request.DiscountValue);
        if (validation is not null)
        {
            return validation;
        }

        var before = ActivityMapper.ToDto(activity);
        activity.Name = request.Name.Trim();
        activity.StartsAt = request.StartsAt;
        activity.EndsAt = request.EndsAt;
        activity.DiscountType = request.DiscountType;
        activity.DiscountValue = request.DiscountValue;
        activity.ApplicableCategories = NormalizeCategories(request.ApplicableCategories);
        activity.IncludeFees = request.IncludeFees;
        activity.IsActive = request.IsActive;
        activity.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        activity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create("update", "activities", activity.Id, activity.Uuid, before, ActivityMapper.ToDto(activity)));
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/toggle")]
    [RequirePermission("Order.Edit")]
    public async Task<IActionResult> ToggleActivity(int id)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(x => x.Id == id);
        if (activity is null)
        {
            return NotFound();
        }

        var before = ActivityMapper.ToDto(activity);
        activity.IsActive = !activity.IsActive;
        activity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create("update", "activities", activity.Id, activity.Uuid, before, ActivityMapper.ToDto(activity)));
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static BadRequestObjectResult? Validate(string name, DateTime startsAt, DateTime endsAt, string discountType, decimal discountValue)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ApiErrors.BadRequest("invalid_activity_name", "Activity name is required.");
        }
        if (endsAt < startsAt)
        {
            return ApiErrors.BadRequest("invalid_activity_period", "Activity end time must be after start time.");
        }
        if (!ValidDiscountTypes.Contains(discountType))
        {
            return ApiErrors.BadRequest("invalid_discount_type", "Unsupported discount type.");
        }
        if (discountValue < 0)
        {
            return ApiErrors.BadRequest("invalid_discount_value", "Discount value cannot be negative.");
        }
        if (discountType == "percent" && discountValue > 100m)
        {
            return ApiErrors.BadRequest("invalid_discount_value", "Percent discount cannot exceed 100.");
        }
        return null;
    }

    private static string NormalizeCategories(string? value)
    {
        return string.Join(",", (value ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x));
    }
}
