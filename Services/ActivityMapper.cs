using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class ActivityMapper
{
    public static ActivityDto ToDto(Activity activity) => new()
    {
        Id = activity.Id,
        Uuid = activity.Uuid,
        Name = activity.Name,
        StartsAt = activity.StartsAt,
        EndsAt = activity.EndsAt,
        DiscountType = activity.DiscountType,
        DiscountValue = activity.DiscountValue,
        ApplicableCategories = activity.ApplicableCategories,
        IncludeFees = activity.IncludeFees,
        IsActive = activity.IsActive,
        Note = activity.Note,
        CreatedAt = activity.CreatedAt,
        UpdatedAt = activity.UpdatedAt
    };
}
