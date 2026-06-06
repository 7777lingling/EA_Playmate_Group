using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class LoginUserService
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly PasswordHasher _passwordHasher;

    public LoginUserService(EAPlaymateGroupDbContext db, PasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<LoginUserDto>> CreateAsync(CreateLoginUserRequestDto request)
    {
        var validationResult = await ValidateAsync(request);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<LoginUserDto>(validationResult);
        }

        var loginUser = new LoginUser
        {
            DisplayName = request.DisplayName.Trim(),
            LoginAccount = request.LoginAccount.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            SystemRole = request.SystemRole,
            IsActive = true
        };

        _db.LoginUsers.Add(loginUser);
        await _db.SaveChangesAsync();

        return ServiceResult<LoginUserDto>.Success(LoginUserMapper.ToDto(loginUser));
    }

    private async Task<ServiceResult> ValidateAsync(CreateLoginUserRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors["displayName"] = ["請輸入顯示名稱。"];
        }

        if (string.IsNullOrWhiteSpace(request.LoginAccount))
        {
            errors["loginAccount"] = ["請輸入登入帳號。"];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["請輸入登入密碼。"];
        }

        if (!DomainValues.IsSystemRole(request.SystemRole))
        {
            errors["systemRole"] = ["系統權限必須是 admin、staff 或 viewer。"];
        }

        if (!string.IsNullOrWhiteSpace(request.LoginAccount))
        {
            var loginAccount = request.LoginAccount.Trim();
            var exists = await _db.LoginUsers.AnyAsync(x => x.LoginAccount == loginAccount);
            if (exists)
            {
                errors["loginAccount"] = ["此登入帳號已存在，請換一個。"];
            }
        }

        return errors.Count > 0 ? ServiceResult.Validation(errors) : ServiceResult.Success();
    }

    private static ServiceResult<T> ToGenericResult<T>(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            return ServiceResult<T>.Validation(result.ValidationErrors);
        }

        return ServiceResult<T>.Failure(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }
}
