using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class UserService
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly AttachmentRequirementService _attachmentRequirementService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(
        EAPlaymateGroupDbContext db,
        AttachmentRequirementService attachmentRequirementService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _attachmentRequirementService = attachmentRequirementService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequestDto request)
    {
        var validationResult = await ValidateUserAsync(request.Nickname);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<UserDto>(validationResult);
        }

        var user = new User
        {
            Nickname = request.Nickname.Trim(),
            BankAccount = string.IsNullOrWhiteSpace(request.BankAccount) ? null : request.BankAccount.Trim(),
            IsPlayer = request.IsPlayer,
            IsBoss = request.IsBoss
        };

        _db.Users.Add(user);
        var saveResult = await SaveUserChangesAsync();
        if (!saveResult.Succeeded)
        {
            return ToGenericResult<UserDto>(saveResult);
        }

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

        var validationResult = await ValidateUserAsync(request.Nickname, id);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var before = UserMapper.ToDto(user);

        user.Nickname = request.Nickname.Trim();
        user.BankAccount = string.IsNullOrWhiteSpace(request.BankAccount) ? null : request.BankAccount.Trim();
        user.IsPlayer = request.IsPlayer;
        user.IsBoss = request.IsBoss;
        user.IsActive = request.IsActive;
        user.LeftAt = request.LeftAt;
        user.UpdatedAt = DateTime.UtcNow;

        var saveResult = await SaveUserChangesAsync();
        if (!saveResult.Succeeded)
        {
            return saveResult;
        }

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

    public async Task<ServiceResult> DeactivateUserAsync(int id, DeactivateUserRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return ServiceResult.Missing();
        }
        if (IsViolation(request.ReasonCategory))
        {
            var attachmentValidation = await _attachmentRequirementService.RequireAsync(
                "users",
                user.Id,
                "attachment_required",
                "Attachment is required when deactivating a member for violation.");
            if (!attachmentValidation.Succeeded)
            {
                return attachmentValidation;
            }
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
        if (IsViolation(request.ReasonCategory))
        {
            var attachmentValidation = await _attachmentRequirementService.RequireAsync(
                "users",
                user.Id,
                "attachment_required",
                "Attachment is required when marking a member leave for violation.");
            if (!attachmentValidation.Succeeded)
            {
                return attachmentValidation;
            }
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

    private async Task<ServiceResult> ValidateUserAsync(
        string nickname,
        int? excludeUserId = null)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(nickname))
        {
            errors["nickname"] = ["請輸入暱稱。"];
        }

        if (!string.IsNullOrWhiteSpace(nickname))
        {
            var normalizedNickname = nickname.Trim();
            var organizationId = _httpContextAccessor.HttpContext?.Session.GetInt32(AuthService.SessionOrganizationId) ?? 0;
            var query = _db.Users
                .IgnoreQueryFilters()
                .Where(x => x.Nickname == normalizedNickname);

            if (organizationId > 0)
            {
                query = query.Where(x => x.OrganizationId == organizationId);
            }

            var nicknameExists = await query.AnyAsync(x =>
                !excludeUserId.HasValue || x.Id != excludeUserId.Value);
            if (nicknameExists)
            {
                errors["nickname"] = ["此暱稱已存在，請換一個。"];
            }
        }

        if (errors.Count > 0)
        {
            return ServiceResult.Validation(errors);
        }

        return ServiceResult.Success();
    }

    private async Task<ServiceResult> SaveUserChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException ex) when (IsUniqueNicknameViolation(ex))
        {
            return ServiceResult.Validation(
                new Dictionary<string, string[]>
                {
                    ["nickname"] = ["此暱稱已存在，請換一個。"]
                });
        }
    }

    private static bool IsUniqueNicknameViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               (sqlException.Number is 2601 or 2627) &&
               (sqlException.Message.Contains("UQ_users_nickname", StringComparison.OrdinalIgnoreCase) ||
                sqlException.Message.Contains("UQ_users_organization_nickname", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsViolation(string? reasonCategory) =>
        string.Equals(reasonCategory, "violation", StringComparison.OrdinalIgnoreCase);

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
