namespace EAPlaymateGroup.Models.DTO;

public sealed class DashboardSummaryDto
{
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal MonthCommissionAmount { get; set; }
    public decimal MonthShareAmount { get; set; }
    public int UnpaidOrderCount { get; set; }
    public int CompletedOrderCount { get; set; }
}

public sealed class MemberRankingDto
{
    public int UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public decimal TotalShareAmount { get; set; }
    public int OrderCount { get; set; }
}

public sealed class MonthlyIncomeDto
{
    public int UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string PayMonth { get; set; } = string.Empty;
    public decimal TotalShareAmount { get; set; }
    public int OrderCount { get; set; }
}
