using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class LoginUserMapper
{
    public static LoginUserDto ToDto(LoginUser user)
    {
        return new LoginUserDto
        {
            Id = user.Id,
            OrganizationId = user.OrganizationId,
            UserId = user.UserId,
            Uuid = user.Uuid,
            DisplayName = user.DisplayName,
            LoginAccount = user.LoginAccount,
            DiscordId = user.DiscordId,
            DiscordName = user.DiscordName,
            DiscordUserId = user.DiscordUserId,
            DiscordLinkedAt = user.DiscordLinkedAt,
            SystemRole = user.SystemRole,
            IsActive = user.IsActive,
            HasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
