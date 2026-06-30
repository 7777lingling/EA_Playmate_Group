using EAPlaymateGroup.Data;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class AttachmentRequirementService
{
    private readonly EAPlaymateGroupDbContext _db;

    public AttachmentRequirementService(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasActiveAttachmentAsync(string targetType, int targetId)
    {
        return await _db.FileAttachments.AsNoTracking()
            .AnyAsync(x => x.TargetType == targetType &&
                           x.TargetId == targetId &&
                           !x.IsDeleted);
    }

    public async Task<ServiceResult> RequireAsync(
        string targetType,
        int targetId,
        string errorCode,
        string message)
    {
        return await HasActiveAttachmentAsync(targetType, targetId)
            ? ServiceResult.Success()
            : ServiceResult.Failure(errorCode, message);
    }
}
