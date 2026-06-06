using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class UserService
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly PasswordHasher _passwordHasher;

    public UserService(EAPlaymateGroupDbContext db, PasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequestDto request)
    {
        var validationResult = ValidateUser(request.Nickname, request.SystemRole);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<UserDto>(validationResult);
        }

        var user = new User
        {
            Nickname = request.Nickname.Trim(),
            DiscordId = string.IsNullOrWhiteSpace(request.DiscordId) ? null : request.DiscordId.Trim(),
            DiscordName = string.IsNullOrWhiteSpace(request.DiscordName) ? null : request.DiscordName.Trim(),
            BankAccount = string.IsNullOrWhiteSpace(request.BankAccount) ? null : request.BankAccount.Trim(),
            LoginAccount = string.IsNullOrWhiteSpace(request.LoginAccount) ? null : request.LoginAccount.Trim(),
            PasswordHash = string.IsNullOrWhiteSpace(request.Password) ? null : _passwordHasher.Hash(request.Password),
            SystemRole = request.SystemRole,
            IsPlayer = request.IsPlayer,
            IsBoss = request.IsBoss
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = UserMapper.ToDto(user);

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "create",
            targetType: "users",
            targetId: user.Id,
            targetUuid: user.Uuid,
            after: dto));
        await _db.SaveChangesAsync();

        return ServiceResult<UserDto>.Success(dto);
    }

    public async Task<ServiceResult> UpdateUserAsync(int id, UpdateUserRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return ServiceResult.Missing();
        }

        var validationResult = ValidateUser(request.Nickname, request.SystemRole);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var before = UserMapper.ToDto(user);

        user.Nickname = request.Nickname.Trim();
        user.DiscordId = string.IsNullOrWhiteSpace(request.DiscordId) ? null : request.DiscordId.Trim();
        user.DiscordName = string.IsNullOrWhiteSpace(request.DiscordName) ? null : request.DiscordName.Trim();
        user.BankAccount = string.IsNullOrWhiteSpace(request.BankAccount) ? null : request.BankAccount.Trim();
        user.LoginAccount = string.IsNullOrWhiteSpace(request.LoginAccount) ? null : request.LoginAccount.Trim();
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }
        user.SystemRole = request.SystemRole;
        user.IsPlayer = request.IsPlayer;
        user.IsBoss = request.IsBoss;
        user.IsActive = request.IsActive;
        user.LeftAt = request.LeftAt;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update",
            targetType: "users",
            targetId: user.Id,
            targetUuid: user.Uuid,
            before: before,
            after: UserMapper.ToDto(user)));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeactivateUserAsync(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return ServiceResult.Missing();
        }

        var before = UserMapper.ToDto(user);

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "deactivate",
            targetType: "users",
            targetId: user.Id,
            targetUuid: user.Uuid,
            before: before,
            after: UserMapper.ToDto(user)));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ActivateUserAsync(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return ServiceResult.Missing();
        }

        var before = UserMapper.ToDto(user);

        user.IsActive = true;
        user.LeftAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "activate",
            targetType: "users",
            targetId: user.Id,
            targetUuid: user.Uuid,
            before: before,
            after: UserMapper.ToDto(user)));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> LeaveUserAsync(int id, LeaveUserRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return ServiceResult.Missing();
        }

        var before = UserMapper.ToDto(user);

        user.IsActive = false;
        user.LeftAt = request.LeftAt ?? DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "leave",
            targetType: "users",
            targetId: user.Id,
            targetUuid: user.Uuid,
            before: before,
            after: UserMapper.ToDto(user)));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private static ServiceResult ValidateUser(string nickname, string systemRole)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return ServiceResult.Validation(new Dictionary<string, string[]>
            {
                ["nickname"] = ["Nickname is required."]
            });
        }

        if (!DomainValues.IsSystemRole(systemRole))
        {
            return ServiceResult.Failure("invalid_system_role", "SystemRole must be admin, staff, or viewer.");
        }

        return ServiceResult.Success();
    }

    private static ServiceResult<T> ToGenericResult<T>(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            return ServiceResult<T>.Validation(result.ValidationErrors);
        }

        if (result.NotFound)
        {
            return ServiceResult<T>.Missing();
        }

        return ServiceResult<T>.Failure(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }
}
