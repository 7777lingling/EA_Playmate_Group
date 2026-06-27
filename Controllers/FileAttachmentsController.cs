using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FileAttachmentsController : ControllerBase
{
    private const long MaxFileSize = 25 * 1024 * 1024;
    private static readonly HashSet<string> TargetTypes =
    [
        "users",
        "login_users",
        "orders",
        "gift_records",
        "payments",
        "service_items",
        "departments"
    ];

    private readonly EAPlaymateGroupDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public FileAttachmentsController(EAPlaymateGroupDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    [RequirePermission("Member.View", "Order.View", "Gift.View", "Settlement.View", "Audit.View")]
    public async Task<ActionResult<List<FileAttachmentDto>>> Get(
        [FromQuery] string targetType,
        [FromQuery] int targetId)
    {
        if (!IsValidTarget(targetType, targetId))
        {
            return ApiErrors.BadRequest("invalid_target", "Invalid attachment target.");
        }

        if (!await TargetExistsAsync(targetType, targetId))
        {
            return NotFound();
        }

        var rows = await _db.FileAttachments.AsNoTracking()
            .Include(x => x.UploadedByLoginUser)
            .Where(x => x.TargetType == targetType && x.TargetId == targetId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    [RequirePermission("Member.Edit", "Order.Edit", "Gift.Edit", "Settlement.Close", "Account.Manage")]
    public async Task<ActionResult<FileAttachmentDto>> Upload(
        [FromForm] string targetType,
        [FromForm] int targetId,
        [FromForm] IFormFile file,
        [FromForm] string? note)
    {
        if (!IsValidTarget(targetType, targetId))
        {
            return ApiErrors.BadRequest("invalid_target", "Invalid attachment target.");
        }

        var targetUuid = await GetTargetUuidAsync(targetType, targetId);
        if (targetUuid is null)
        {
            return NotFound();
        }

        if (file.Length <= 0 || file.Length > MaxFileSize)
        {
            return ApiErrors.BadRequest("invalid_file_size", "File size must be between 1 byte and 25 MB.");
        }

        var originalFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return ApiErrors.BadRequest("invalid_file_name", "File name is required.");
        }

        var extension = Path.GetExtension(originalFileName);
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
        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new FileAttachment
        {
            TargetType = targetType,
            TargetId = targetId,
            TargetUuid = targetUuid,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            StoragePath = relativePath,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            FileSize = file.Length,
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
                attachment.OriginalFileName,
                attachment.FileSize,
                attachment.Note
            }));
        await _db.SaveChangesAsync();

        await _db.Entry(attachment).Reference(x => x.UploadedByLoginUser).LoadAsync();
        return CreatedAtAction(nameof(Download), new { id = attachment.Id }, ToDto(attachment));
    }

    [HttpGet("{id:long}/download")]
    [RequirePermission("Member.View", "Order.View", "Gift.View", "Settlement.View", "Audit.View")]
    public async Task<IActionResult> Download(long id)
    {
        var attachment = await _db.FileAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (attachment is null)
        {
            return NotFound();
        }

        var path = Path.Combine(_environment.ContentRootPath, attachment.StoragePath);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return PhysicalFile(path, attachment.ContentType, attachment.OriginalFileName);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("Member.Edit", "Order.Edit", "Gift.Edit", "Settlement.Close", "Account.Manage")]
    public async Task<IActionResult> Delete(long id)
    {
        var attachment = await _db.FileAttachments.FirstOrDefaultAsync(x => x.Id == id);
        if (attachment is null)
        {
            return NotFound();
        }

        var before = ToDto(attachment);
        _db.FileAttachments.Remove(attachment);
        _db.AuditLogs.Add(AuditLogWriter.Create(
            "delete",
            "file_attachments",
            ToNullableInt(attachment.Id),
            before: before));
        await _db.SaveChangesAsync();

        var path = Path.Combine(_environment.ContentRootPath, attachment.StoragePath);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return NoContent();
    }

    private static FileAttachmentDto ToDto(FileAttachment attachment) => new()
    {
        Id = attachment.Id,
        TargetType = attachment.TargetType,
        TargetId = attachment.TargetId,
        TargetUuid = attachment.TargetUuid,
        OriginalFileName = attachment.OriginalFileName,
        ContentType = attachment.ContentType,
        FileSize = attachment.FileSize,
        UploadedByLoginUserId = attachment.UploadedByLoginUserId,
        UploadedByDisplayName = attachment.UploadedByLoginUser?.DisplayName,
        Note = attachment.Note,
        CreatedAt = attachment.CreatedAt
    };

    private async Task<bool> TargetExistsAsync(string targetType, int targetId) =>
        await GetTargetUuidAsync(targetType, targetId) is not null;

    private async Task<Guid?> GetTargetUuidAsync(string targetType, int targetId)
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
            "payments" => await _db.Payments.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "service_items" => await _db.ServiceItems.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            "departments" => await _db.Departments.AsNoTracking()
                .Where(x => x.Id == targetId)
                .Select(x => (Guid?)x.Uuid)
                .FirstOrDefaultAsync(),
            _ => null
        };
    }

    private static bool IsValidTarget(string targetType, int targetId) =>
        targetId > 0 && TargetTypes.Contains(targetType);

    private static int? ToNullableInt(long value) =>
        value <= int.MaxValue ? (int)value : null;
}
