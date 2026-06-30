using System.Security.Cryptography;
using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class FileAttachmentService
{
    public const long MaxFileSize = 25 * 1024 * 1024;

    private static readonly HashSet<string> TargetTypes =
    [
        "users",
        "login_users",
        "orders",
        "gift_records",
        "money_logs",
        "payments",
        "audit_logs",
        "login_histories",
        "department_members",
        "organizations",
        "service_items",
        "departments"
    ];

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".gif",
        ".pdf", ".docx", ".xlsx", ".pptx", ".csv",
        ".txt", ".log",
        ".mp4", ".mov"
    ];

    private readonly EAPlaymateGroupDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public FileAttachmentService(EAPlaymateGroupDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public static bool IsValidTarget(string targetType, int targetId) =>
        targetId > 0 && TargetTypes.Contains(targetType);

    public ServiceResult ValidateFiles(IReadOnlyCollection<IFormFile> files)
    {
        if (files.Count == 0)
        {
            return ServiceResult.Failure("attachment_required", "At least one attachment is required.");
        }

        foreach (var file in files)
        {
            var result = ValidateFile(file);
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return ServiceResult.Success();
    }

    public async Task<bool> TargetExistsAsync(string targetType, int targetId) =>
        targetType switch
        {
            "users" => await _db.Users.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "login_users" => await _db.LoginUsers.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "orders" => await _db.Orders.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "gift_records" => await _db.GiftRecords.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "money_logs" => await _db.MoneyLogs.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "payments" => await _db.Payments.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "audit_logs" => await _db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "login_histories" => await _db.LoginHistories.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "departments" => await _db.Departments.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "department_members" => await _db.DepartmentMembers.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "organizations" => await _db.Organizations.AsNoTracking().AnyAsync(x => x.Id == targetId),
            "service_items" => await _db.ServiceItems.AsNoTracking().AnyAsync(x => x.Id == targetId),
            _ => false
        };

    public async Task<Guid?> GetTargetUuidAsync(string targetType, int targetId)
    {
        return targetType switch
        {
            "users" => await _db.Users.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "login_users" => await _db.LoginUsers.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "orders" => await _db.Orders.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "gift_records" => await _db.GiftRecords.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "money_logs" => await _db.MoneyLogs.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => x.SourceUuid)
                .FirstOrDefaultAsync(),
            "payments" => await _db.Payments.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "audit_logs" => await _db.AuditLogs.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.CorrelationId)
                .FirstOrDefaultAsync(),
            "login_histories" => null,
            "service_items" => await _db.ServiceItems.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "departments" => await _db.Departments.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "department_members" => null,
            "organizations" => null,
            _ => null
        };
    }

    public async Task<ServiceResult<FileAttachmentDto>> UploadAsync(
        string targetType,
        int targetId,
        IFormFile file,
        string? attachmentKind,
        string? note)
    {
        if (!IsValidTarget(targetType, targetId))
        {
            return ServiceResult<FileAttachmentDto>.Failure("invalid_target", "Invalid attachment target.");
        }

        if (!await TargetExistsAsync(targetType, targetId))
        {
            return ServiceResult<FileAttachmentDto>.Missing();
        }

        var validation = ValidateFile(file);
        if (!validation.Succeeded)
        {
            return ServiceResult<FileAttachmentDto>.Failure(
                validation.ErrorCode ?? "invalid_file",
                validation.ErrorMessage ?? "Invalid file.");
        }

        var attachment = await SaveAttachmentAsync(
            targetType,
            targetId,
            await GetTargetUuidAsync(targetType, targetId),
            file,
            attachmentKind,
            note);

        await _db.Entry(attachment).Reference(x => x.UploadedByLoginUser).LoadAsync();
        return ServiceResult<FileAttachmentDto>.Success(ToDto(attachment));
    }

    public async Task<List<FileAttachment>> UploadManyAsync(
        string targetType,
        int targetId,
        IReadOnlyCollection<IFormFile> files,
        string? attachmentKind,
        string? note)
    {
        var targetUuid = await GetTargetUuidAsync(targetType, targetId);
        var attachments = new List<FileAttachment>();
        foreach (var file in files)
        {
            attachments.Add(await SaveAttachmentAsync(targetType, targetId, targetUuid, file, attachmentKind, note));
        }

        return attachments;
    }

    public static FileAttachmentDto ToDto(FileAttachment attachment) => new()
    {
        Id = attachment.Id,
        TargetType = attachment.TargetType,
        TargetId = attachment.TargetId,
        TargetUuid = attachment.TargetUuid,
        AttachmentKind = attachment.AttachmentKind,
        OriginalFileName = attachment.OriginalFileName,
        ContentType = attachment.ContentType,
        FileExtension = attachment.FileExtension,
        FileSize = attachment.FileSize,
        Sha256Hash = attachment.Sha256Hash,
        UploadedByLoginUserId = attachment.UploadedByLoginUserId,
        UploadedByDisplayName = attachment.UploadedByLoginUser?.DisplayName,
        Note = attachment.Note,
        IsDeleted = attachment.IsDeleted,
        DeletedAt = attachment.DeletedAt,
        DeletedByLoginUserId = attachment.DeletedByLoginUserId,
        CreatedAt = attachment.CreatedAt
    };

    private async Task<FileAttachment> SaveAttachmentAsync(
        string targetType,
        int targetId,
        Guid? targetUuid,
        IFormFile file,
        string? attachmentKind,
        string? note)
    {
        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (extension.Length > 20)
        {
            extension = string.Empty;
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeDirectory = Path.Combine("FileAttachments", targetType, targetId.ToString());
        var absoluteDirectory = Path.Combine(_environment.ContentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var relativePath = Path.Combine(relativeDirectory, storedFileName);
        var absolutePath = Path.Combine(_environment.ContentRootPath, relativePath);
        string sha256Hash;
        await using (var stream = File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }
        await using (var readStream = File.OpenRead(absolutePath))
        {
            sha256Hash = Convert.ToHexString(await SHA256.HashDataAsync(readStream)).ToLowerInvariant();
        }

        var attachment = new FileAttachment
        {
            TargetType = targetType,
            TargetId = targetId,
            TargetUuid = targetUuid,
            AttachmentKind = string.IsNullOrWhiteSpace(attachmentKind) ? null : attachmentKind.Trim(),
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            StoragePath = relativePath,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            FileExtension = extension.TrimStart('.'),
            FileSize = file.Length,
            Sha256Hash = sha256Hash,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
        _db.FileAttachments.Add(attachment);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            "upload",
            "file_attachments",
            ToNullableInt(attachment.Id),
            after: new
            {
                attachment.TargetType,
                attachment.TargetId,
                attachment.AttachmentKind,
                attachment.OriginalFileName,
                attachment.FileExtension,
                attachment.FileSize,
                attachment.Sha256Hash,
                attachment.Note
            }));
        await _db.SaveChangesAsync();

        return attachment;
    }

    private static ServiceResult ValidateFile(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxFileSize)
        {
            return ServiceResult.Failure("invalid_file_size", "File size must be between 1 byte and 25 MB.");
        }

        var originalFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return ServiceResult.Failure("invalid_file_name", "File name is required.");
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (extension.Length > 20)
        {
            extension = string.Empty;
        }
        if (!AllowedExtensions.Contains(extension))
        {
            return ServiceResult.Failure("unsupported_file_type", "Unsupported file type.");
        }

        return ServiceResult.Success();
    }

    private static int? ToNullableInt(long value) =>
        value <= int.MaxValue ? (int)value : null;
}
