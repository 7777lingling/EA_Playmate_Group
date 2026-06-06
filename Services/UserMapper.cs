using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class UserMapper
{
    public static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Uuid = user.Uuid,
            Nickname = user.Nickname,
            DiscordId = user.DiscordId,
            DiscordName = user.DiscordName,
            BankAccount = user.BankAccount,
            SystemRole = user.SystemRole,
            IsPlayer = user.IsPlayer,
            IsBoss = user.IsBoss,
            IsActive = user.IsActive,
            LeftAt = user.LeftAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
