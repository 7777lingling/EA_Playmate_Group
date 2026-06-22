using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MoneyLogsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly MoneyLogService _moneyLogService;

    public MoneyLogsController(EAPlaymateGroupDbContext db, MoneyLogService moneyLogService)
    {
        _db = db;
        _moneyLogService = moneyLogService;
    }

    [HttpGet]
    [RequirePermission("Audit.View")]
    public async Task<ActionResult<List<MoneyLogDto>>> Get([FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);
        var rows = await _db.MoneyLogs.AsNoTracking()
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync();
        return rows.Select(MoneyLogService.ToDto).ToList();
    }

    [HttpPost]
    [RequirePermission("Settlement.Close")]
    public async Task<ActionResult<MoneyLogDto>> Create(CreateMoneyLogRequestDto request)
    {
        var result = await _moneyLogService.AddManualAsync(request);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.NotFound
            ? NotFound()
            : ApiErrors.BadRequest(result.ErrorCode ?? "operation_failed", result.ErrorMessage ?? "操作失敗。");
    }
}
