using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class ServiceItemMapper
{
    public static ServiceItemDto ToDto(ServiceItem item)
    {
        return new ServiceItemDto
        {
            Id = item.Id,
            Uuid = item.Uuid,
            SeedKey = item.SeedKey,
            Category = item.Category,
            Subcategory = item.Subcategory,
            Name = item.Name,
            UnitType = item.UnitType,
            DefaultPrice = item.DefaultPrice,
            PriceNote = item.PriceNote,
            Remark = item.Remark,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive
        };
    }
}
