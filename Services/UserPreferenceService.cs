using System.Text.RegularExpressions;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class UserPreferenceService
{
    private static readonly Regex HexColorPattern = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    private static readonly HashSet<string> ThemeNames =
    [
        "purple-tech",
        "blue-metal",
        "dopamine-candy",
        "mint-energy",
        "sunset-neon",
        "light-clean"
    ];

    private readonly EAPlaymateGroupDbContext _db;

    public UserPreferenceService(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    public async Task<UserPreferenceDto?> GetAsync(int loginUserId)
    {
        var preference = await GetOrCreateEntityAsync(loginUserId);
        return preference is null ? null : ToDto(preference);
    }

    public async Task<ServiceResult<UserPreferenceDto>> UpdateAsync(
        int loginUserId,
        UpdateUserPreferenceRequestDto request)
    {
        var validation = Validate(request);
        if (validation.Count > 0)
        {
            return ServiceResult<UserPreferenceDto>.Validation(validation);
        }

        var preference = await GetOrCreateEntityAsync(loginUserId);
        if (preference is null)
        {
            return ServiceResult<UserPreferenceDto>.Missing();
        }

        preference.ThemeName = NormalizeThemeName(request.ThemeName);
        preference.AccentColor = NormalizeAccentColor(request.AccentColor);
        preference.DashboardLayout = string.IsNullOrWhiteSpace(request.DashboardLayout)
            ? null
            : request.DashboardLayout.Trim();
        preference.TablePageSize = NormalizePageSize(request.TablePageSize);
        preference.DefaultOrderStatusFilter = NormalizeNullable(request.DefaultOrderStatusFilter);
        preference.DefaultMoneyLogFilter = NormalizeNullable(request.DefaultMoneyLogFilter);
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ServiceResult<UserPreferenceDto>.Success(ToDto(preference));
    }

    private async Task<UserPreference?> GetOrCreateEntityAsync(int loginUserId)
    {
        var preference = await _db.UserPreferences.FirstOrDefaultAsync(x => x.LoginUserId == loginUserId);
        if (preference is not null)
        {
            return preference;
        }

        var loginUser = await _db.LoginUsers.FirstOrDefaultAsync(x => x.Id == loginUserId && x.IsActive);
        if (loginUser is null)
        {
            return null;
        }

        preference = new UserPreference
        {
            LoginUserId = loginUser.Id,
            ThemeName = "purple-tech",
            TablePageSize = 100
        };
        _db.UserPreferences.Add(preference);
        await _db.SaveChangesAsync();
        return preference;
    }

    private static Dictionary<string, string[]> Validate(UpdateUserPreferenceRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();
        var themeName = string.IsNullOrWhiteSpace(request.ThemeName)
            ? "purple-tech"
            : request.ThemeName.Trim();
        if (!ThemeNames.Contains(themeName))
        {
            errors["themeName"] = ["不支援的主題。"];
        }

        if (!string.IsNullOrWhiteSpace(request.AccentColor) &&
            !HexColorPattern.IsMatch(request.AccentColor.Trim()))
        {
            errors["accentColor"] = ["請輸入 #RRGGBB 色碼。"];
        }

        if (request.TablePageSize is < 20 or > 500)
        {
            errors["tablePageSize"] = ["表格筆數需介於 20 到 500。"];
        }

        return errors;
    }

    private static string NormalizeThemeName(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "purple-tech"
            : value.Trim();
        return ThemeNames.Contains(normalized) ? normalized : "purple-tech";
    }

    private static string? NormalizeAccentColor(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int NormalizePageSize(int value) => Math.Clamp(value, 20, 500);

    public static UserPreferenceDto ToDto(UserPreference preference) => new()
    {
        Id = preference.Id,
        LoginUserId = preference.LoginUserId,
        ThemeName = preference.ThemeName,
        AccentColor = preference.AccentColor,
        DashboardLayout = preference.DashboardLayout,
        TablePageSize = preference.TablePageSize,
        DefaultOrderStatusFilter = preference.DefaultOrderStatusFilter,
        DefaultMoneyLogFilter = preference.DefaultMoneyLogFilter,
        CreatedAt = preference.CreatedAt,
        UpdatedAt = preference.UpdatedAt
    };
}
