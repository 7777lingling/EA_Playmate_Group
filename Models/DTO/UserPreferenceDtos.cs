namespace EAPlaymateGroup.Models.DTO;

public sealed class UserPreferenceDto
{
    public int Id { get; set; }
    public int LoginUserId { get; set; }
    public string ThemeName { get; set; } = "purple-tech";
    public string? AccentColor { get; set; }
    public string? DashboardLayout { get; set; }
    public int TablePageSize { get; set; } = 100;
    public string? DefaultOrderStatusFilter { get; set; }
    public string? DefaultMoneyLogFilter { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class UpdateUserPreferenceRequestDto
{
    public string ThemeName { get; set; } = "purple-tech";
    public string? AccentColor { get; set; }
    public string? DashboardLayout { get; set; }
    public int TablePageSize { get; set; } = 100;
    public string? DefaultOrderStatusFilter { get; set; }
    public string? DefaultMoneyLogFilter { get; set; }
}
