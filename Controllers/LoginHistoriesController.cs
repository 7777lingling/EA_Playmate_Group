using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequirePermission("Audit.View")]
public sealed class LoginHistoriesController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public LoginHistoriesController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [RequirePermission("Audit.View")]
    public async Task<ActionResult<List<LoginHistoryDto>>> Get(
        [FromQuery] int? loginUserId,
        [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        var query = _db.LoginHistories.AsNoTracking()
            .Include(x => x.LoginUser)
            .AsQueryable();

        if (loginUserId.HasValue)
        {
            query = query.Where(x => x.LoginUserId == loginUserId.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(rows);
    }

    private static LoginHistoryDto ToDto(LoginHistory row) => new()
    {
        Id = row.Id,
        LoginUserId = row.LoginUserId,
        LoginUserDisplayName = row.LoginUser.DisplayName,
        LoginAccount = row.LoginUser.LoginAccount,
        Action = row.Action,
        Method = row.Method,
        IpAddress = row.IpAddress,
        UserAgent = row.UserAgent,
        SessionId = row.SessionId,
        DeviceInfo = row.DeviceInfo,
        FailureReason = row.FailureReason,
        Succeeded = row.Succeeded,
        LoggedOutAt = row.LoggedOutAt,
        DurationSeconds = row.DurationSeconds,
        CreatedAt = row.CreatedAt
    };
}
