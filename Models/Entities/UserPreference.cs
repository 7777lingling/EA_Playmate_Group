namespace EAPlaymateGroup.Models.Entities;

public sealed class UserPreference
{
    public int Id { get; set; }
    public int LoginUserId { get; set; }
    public LoginUser LoginUser { get; set; } = null!;
    public string ThemeName { get; set; } = "purple-tech";
    public string? AccentColor { get; set; }
    public string? DashboardLayout { get; set; }
    public int TablePageSize { get; set; } = 100;
    public string? DefaultOrderStatusFilter { get; set; }
    public string? DefaultMoneyLogFilter { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
