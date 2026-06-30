using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FileAttachmentsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly FileAttachmentService _fileAttachmentService;

    public FileAttachmentsController(
        EAPlaymateGroupDbContext db,
        IWebHostEnvironment environment,
        FileAttachmentService fileAttachmentService)
    {
        _db = db;
        _environment = environment;
        _fileAttachmentService = fileAttachmentService;
    }

    [HttpGet]
    [RequirePermission("Member.View", "Order.View", "Gift.View", "Settlement.View", "Audit.View")]
    public async Task<ActionResult<List<FileAttachmentDto>>> Get(
        [FromQuery] string targetType,
        [FromQuery] int targetId)
    {
        if (!FileAttachmentService.IsValidTarget(targetType, targetId))
        {
            return ApiErrors.BadRequest("invalid_target", "Invalid attachment target.");
        }

        if (!await _fileAttachmentService.TargetExistsAsync(targetType, targetId))
        {
            return NotFound();
        }

        var rows = await _db.FileAttachments.AsNoTracking()
            .Include(x => x.UploadedByLoginUser)
            .Where(x => x.TargetType == targetType && x.TargetId == targetId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => FileAttachmentService.ToDto(x))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost]
    [RequestSizeLimit(FileAttachmentService.MaxFileSize + 1024 * 1024)]
    [RequirePermission("Member.Edit", "Order.Edit", "Gift.Edit", "Settlement.Close", "Account.Manage")]
    public async Task<ActionResult<FileAttachmentDto>> Upload(
        [FromForm] string targetType,
        [FromForm] int targetId,
        [FromForm] IFormFile file,
        [FromForm] string? attachmentKind,
        [FromForm] string? note)
    {
        var result = await _fileAttachmentService.UploadAsync(targetType, targetId, file, attachmentKind, note);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(Download), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpGet("{id:long}/download")]
    [RequirePermission("Member.View", "Order.View", "Gift.View", "Settlement.View", "Audit.View")]
    public async Task<IActionResult> Download(long id)
    {
        var attachment = await _db.FileAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (attachment is null)
        {
            return NotFound();
        }

        var path = Path.Combine(_environment.ContentRootPath, attachment.StoragePath);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        _db.AuditLogs.Add(AuditLogWriter.Create(
            "download",
            "file_attachments",
            ToNullableInt(attachment.Id),
            after: new
            {
                attachment.TargetType,
                attachment.TargetId,
                attachment.OriginalFileName,
                attachment.FileSize
            }));
        await _db.SaveChangesAsync();

        return PhysicalFile(path, attachment.ContentType, attachment.OriginalFileName);
    }

    [HttpGet("{id:long}/preview")]
    [RequirePermission("Member.View", "Order.View", "Gift.View", "Settlement.View", "Audit.View")]
    public async Task<IActionResult> Preview(long id)
    {
        var attachment = await _db.FileAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (attachment is null)
        {
            return NotFound();
        }

        var path = Path.Combine(_environment.ContentRootPath, attachment.StoragePath);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        _db.AuditLogs.Add(AuditLogWriter.Create(
            "preview",
            "file_attachments",
            ToNullableInt(attachment.Id),
            after: new
            {
                attachment.TargetType,
                attachment.TargetId,
                attachment.OriginalFileName,
                attachment.FileSize
            }));
        await _db.SaveChangesAsync();

        return PhysicalFile(path, attachment.ContentType);
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

        var before = FileAttachmentService.ToDto(attachment);
        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;
        _db.AuditLogs.Add(AuditLogWriter.Create(
            "delete",
            "file_attachments",
            ToNullableInt(attachment.Id),
            before: before));
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private ActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.NotFound)
        {
            return NotFound();
        }

        return ApiErrors.BadRequest(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }

    private static int? ToNullableInt(long value) =>
        value <= int.MaxValue ? (int)value : null;
}
