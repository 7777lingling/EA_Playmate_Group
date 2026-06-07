using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ServiceItemsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public ServiceItemsController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceItemDto>>> GetServiceItems([FromQuery] bool activeOnly = false)
    {
        var query = _db.ServiceItems.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return Ok(items.Select(ServiceItemMapper.ToDto).ToList());
    }
}
