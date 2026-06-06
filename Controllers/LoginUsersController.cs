using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LoginUsersController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly LoginUserService _loginUserService;

    public LoginUsersController(EAPlaymateGroupDbContext db, LoginUserService loginUserService)
    {
        _db = db;
        _loginUserService = loginUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LoginUserDto>>> GetLoginUsers()
    {
        var users = await _db.LoginUsers.AsNoTracking()
            .OrderBy(x => x.LoginAccount)
            .ToListAsync();

        return Ok(users.Select(LoginUserMapper.ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoginUserDto>> GetLoginUser(int id)
    {
        var user = await _db.LoginUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return user is null ? NotFound() : Ok(LoginUserMapper.ToDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<LoginUserDto>> CreateLoginUser(CreateLoginUserRequestDto request)
    {
        var result = await _loginUserService.CreateAsync(request);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetLoginUser), new { id = result.Value!.Id }, result.Value);
        }

        return result.ValidationErrors is not null
            ? ApiErrors.Validation(result.ValidationErrors)
            : ApiErrors.BadRequest(result.ErrorCode ?? "operation_failed", result.ErrorMessage ?? "Operation failed.");
    }
}
