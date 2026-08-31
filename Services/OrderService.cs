using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class OrderService
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly AttachmentRequirementService _attachmentRequirementService;
    private readonly FileAttachmentService _fileAttachmentService;

    public OrderService(
        EAPlaymateGroupDbContext db,
        AttachmentRequirementService attachmentRequirementService,
        FileAttachmentService fileAttachmentService)
    {
        _db = db;
        _attachmentRequirementService = attachmentRequirementService;
        _fileAttachmentService = fileAttachmentService;
    }

    public async Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderRequestDto request)
    {
        var validationResult = await ValidateCreateOrderAsync(request);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<OrderDto>(validationResult);
        }
        if (request.Status == "disputed" || request.CustomerPaymentStatus is "paid" or "partial")
        {
            return ServiceResult<OrderDto>.Failure(
                "attachment_required",
                "Attachment is required before creating a disputed or paid order. Create the order first, upload attachments, then update status.");
        }

        var commission = await ResolveCommissionAsync(request);

        var order = new Order
        {
            OrderNo = string.IsNullOrWhiteSpace(request.OrderNo) ? null : request.OrderNo.Trim(),
            OrderType = string.IsNullOrWhiteSpace(request.OrderType) ? "boosting" : request.OrderType,
            OrderDate = request.OrderDate,
            OwnerUserId = request.OwnerUserId,
            Amount = request.Amount,
            ServiceQuantity = request.ServiceQuantity,
            CommissionRate = commission.Rate,
            CommissionAmount = commission.Amount,
            Status = request.Status,
            CustomerPaymentStatus = request.CustomerPaymentStatus,
            Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
            Members = request.Members.Select(x => new OrderMember
            {
                UserId = x.UserId,
                Role = x.Role,
                ShareAmount = x.ShareAmount
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var savedOrder = await GetOrderWithRelations(order.Id).FirstAsync();
        var dto = OrderMapper.ToDto(savedOrder);

        var audit = AuditLogWriter.Create(
            action: "create",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            after: dto);
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();

        order.CreatedAuditLogId = audit.Id;
        await _db.SaveChangesAsync();

        return ServiceResult<OrderDto>.Success(dto);
    }

    public async Task<ServiceResult<OrderDto>> CreateOrderWithAttachmentsAsync(
        CreateOrderRequestDto request,
        IReadOnlyCollection<IFormFile> attachments)
    {
        var validationResult = await ValidateCreateOrderAsync(request);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<OrderDto>(validationResult);
        }

        if ((request.Status == "disputed" || request.CustomerPaymentStatus is "paid" or "partial") && attachments.Count == 0)
        {
            return ServiceResult<OrderDto>.Failure(
                "attachment_required",
                "Attachment is required when creating a disputed, paid, or partial order.");
        }

        if (attachments.Count > 0)
        {
            var attachmentValidation = _fileAttachmentService.ValidateFiles(attachments);
            if (!attachmentValidation.Succeeded)
            {
                return ServiceResult<OrderDto>.Failure(
                    attachmentValidation.ErrorCode ?? "invalid_attachment",
                    attachmentValidation.ErrorMessage ?? "Invalid attachment.");
            }
        }

        var commission = await ResolveCommissionAsync(request);

        var order = new Order
        {
            OrderNo = string.IsNullOrWhiteSpace(request.OrderNo) ? null : request.OrderNo.Trim(),
            OrderType = string.IsNullOrWhiteSpace(request.OrderType) ? "boosting" : request.OrderType,
            OrderDate = request.OrderDate,
            OwnerUserId = request.OwnerUserId,
            Amount = request.Amount,
            ServiceQuantity = request.ServiceQuantity,
            CommissionRate = commission.Rate,
            CommissionAmount = commission.Amount,
            Status = request.Status,
            CustomerPaymentStatus = request.CustomerPaymentStatus,
            Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
            Members = request.Members.Select(x => new OrderMember
            {
                UserId = x.UserId,
                Role = x.Role,
                ShareAmount = x.ShareAmount
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        if (attachments.Count > 0)
        {
            var savedAttachments = await _fileAttachmentService.UploadManyAsync(
                "orders",
                order.Id,
                attachments,
                order.CustomerPaymentStatus is "paid" or "partial"
                    ? "payment_proof"
                    : order.Status == "disputed"
                        ? "evidence"
                        : "general",
                order.Remark);

            _db.AuditLogs.Add(AuditLogWriter.Create(
                "bind_attachments",
                "orders",
                order.Id,
                order.Uuid,
                after: new
                {
                    attachmentIds = savedAttachments.Select(x => x.Id).ToList(),
                    attachmentCount = savedAttachments.Count
                }));
            await _db.SaveChangesAsync();
        }

        var savedOrder = await GetOrderWithRelations(order.Id).FirstAsync();
        var dto = OrderMapper.ToDto(savedOrder);

        var audit = AuditLogWriter.Create(
            action: "create",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            after: dto);
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();

        order.CreatedAuditLogId = audit.Id;
        await _db.SaveChangesAsync();

        return ServiceResult<OrderDto>.Success(dto);
    }

    public async Task<ServiceResult> UpdateOrderAsync(int id, UpdateOrderRequestDto request)
    {
        var order = await _db.Orders
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
        {
            return ServiceResult.Missing();
        }

        var validationResult = await ValidateUpdateOrderAsync(order.Id, request);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }
        var attachmentValidation = await ValidateRequiredAttachmentsAsync(order.Id, request.Status, request.CustomerPaymentStatus);
        if (!attachmentValidation.Succeeded)
        {
            return attachmentValidation;
        }

        var before = OrderMapper.ToDto(order);

        order.OrderNo = string.IsNullOrWhiteSpace(request.OrderNo) ? null : request.OrderNo.Trim();
        order.OrderType = string.IsNullOrWhiteSpace(request.OrderType) ? "boosting" : request.OrderType;
        order.OrderDate = request.OrderDate;
        order.OwnerUserId = request.OwnerUserId;
        order.Amount = request.Amount;
        order.ServiceQuantity = request.ServiceQuantity;
        var commission = await ResolveCommissionAsync(request, order.Id);
        order.CommissionRate = commission.Rate;
        order.CommissionAmount = commission.Amount;
        order.Status = request.Status;
        order.CustomerPaymentStatus = request.CustomerPaymentStatus;
        order.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderMembers.RemoveRange(order.Members);
        order.Members = request.Members.Select(x => new OrderMember
        {
            OrderId = order.Id,
            UserId = x.UserId,
            Role = x.Role,
            ShareAmount = x.ShareAmount
        }).ToList();

        await _db.SaveChangesAsync();

        var savedOrder = await GetOrderWithRelations(order.Id).FirstAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: OrderMapper.ToDto(savedOrder)));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelOrderAsync(int id, UpdateOrderStatusRequestDto request)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return ServiceResult.Missing();
        }
        if (request.Status == "disputed")
        {
            var attachmentValidation = await _attachmentRequirementService.RequireAsync(
                "orders",
                order.Id,
                "attachment_required",
                "Attachment is required when changing an order to disputed.");
            if (!attachmentValidation.Succeeded)
            {
                return attachmentValidation;
            }
        }

        var before = new
        {
            order.Status,
            order.Remark
        };

        order.Status = "cancelled";
        order.Remark = string.IsNullOrWhiteSpace(request.Remark) ? order.Remark : request.Remark.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var audit = AuditLogWriter.Create(
            action: "cancel",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: new
            {
                order.Status,
                order.Remark
            });
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();

        order.CancelledAuditLogId = audit.Id;
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateStatusAsync(int id, UpdateOrderStatusRequestDto request)
    {
        if (!DomainValues.IsOrderStatus(request.Status))
        {
            return ServiceResult.Failure("invalid_order_status", "Invalid order status.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return ServiceResult.Missing();
        }
        if (request.Status == "disputed")
        {
            var attachmentValidation = await _attachmentRequirementService.RequireAsync(
                "orders",
                order.Id,
                "attachment_required",
                "Attachment is required when changing an order to disputed.");
            if (!attachmentValidation.Succeeded)
            {
                return attachmentValidation;
            }
        }

        var before = new
        {
            order.Status,
            order.Remark
        };

        order.Status = request.Status;
        order.Remark = string.IsNullOrWhiteSpace(request.Remark) ? order.Remark : request.Remark.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update_status",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: new
            {
                order.Status,
                order.Remark
            }));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateCustomerPaymentStatusAsync(int id, UpdateCustomerPaymentStatusRequestDto request)
    {
        if (!DomainValues.IsCustomerPaymentStatus(request.CustomerPaymentStatus))
        {
            return ServiceResult.Failure("invalid_customer_payment_status", "Invalid customer payment status.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return ServiceResult.Missing();
        }
        if (request.CustomerPaymentStatus is "paid" or "partial")
        {
            var attachmentValidation = await _attachmentRequirementService.RequireAsync(
                "orders",
                order.Id,
                "attachment_required",
                "Attachment is required when changing order payment status to paid or partial.");
            if (!attachmentValidation.Succeeded)
            {
                return attachmentValidation;
            }
        }

        var before = new
        {
            order.CustomerPaymentStatus
        };

        order.CustomerPaymentStatus = request.CustomerPaymentStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            action: "update_customer_payment_status",
            targetType: "orders",
            targetId: order.Id,
            targetUuid: order.Uuid,
            before: before,
            after: new
            {
                order.CustomerPaymentStatus
            }));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private async Task<ServiceResult> ValidateCreateOrderAsync(CreateOrderRequestDto request)
    {
        var commission = await ResolveCommissionAsync(request);

        return await ValidateOrderAsync(
            request.Amount,
            commission.Amount,
            request.ServiceQuantity,
            request.OrderType,
            request.Status,
            request.CustomerPaymentStatus,
            request.OwnerUserId,
            request.Members);
    }

    private async Task<ServiceResult> ValidateUpdateOrderAsync(int orderId, UpdateOrderRequestDto request)
    {
        return await ValidateOrderAsync(
            request.Amount,
            (await ResolveCommissionAsync(request, orderId)).Amount,
            request.ServiceQuantity,
            request.OrderType,
            request.Status,
            request.CustomerPaymentStatus,
            request.OwnerUserId,
            request.Members);
    }

    private async Task<ServiceResult> ValidateOrderAsync(
        decimal amount,
        decimal commissionAmount,
        decimal serviceQuantity,
        string orderType,
        string status,
        string customerPaymentStatus,
        int? ownerUserId,
        IReadOnlyCollection<CreateOrderMemberRequestDto> members)
    {
        if (amount <= 0)
        {
            return ServiceResult.Failure("invalid_amount", "Amount must be greater than zero.");
        }

        if (serviceQuantity < 0)
        {
            return ServiceResult.Failure("invalid_service_quantity", "Service quantity must be zero or greater.");
        }

        if (orderType == "companion" && serviceQuantity <= 0)
        {
            return ServiceResult.Failure("invalid_service_quantity", "Companion orders must include service hours.");
        }

        if (members.Count == 0)
        {
            return ServiceResult.Failure("missing_order_members", "At least one order member is required.");
        }

        var valueErrors = ValidateOrderValues(orderType, status, customerPaymentStatus, members.Select(x => x.Role));
        if (valueErrors.Count > 0)
        {
            return ServiceResult.Validation(valueErrors);
        }

        if (commissionAmount < 0 || commissionAmount > amount)
        {
            return ServiceResult.Failure("invalid_commission_amount", "Commission amount must be between zero and amount.");
        }

        var distributableAmount = amount - commissionAmount;
        var shareTotal = members.Sum(x => x.ShareAmount);
        if (shareTotal != distributableAmount)
        {
            return ServiceResult.Failure("invalid_share_total", $"Share total must equal amount - commission amount. Expected {distributableAmount}, got {shareTotal}.");
        }

        var userIds = members.Select(x => x.UserId).Distinct().ToList();
        var validUserCount = await _db.Users.CountAsync(x => userIds.Contains(x.Id) && x.IsActive);
        if (validUserCount != userIds.Count)
        {
            return ServiceResult.Failure("invalid_order_member", "One or more order members do not exist or are inactive.");
        }

        if (ownerUserId.HasValue)
        {
            var ownerExists = await _db.Users.AnyAsync(x => x.Id == ownerUserId.Value && x.IsActive);
            if (!ownerExists)
            {
                return ServiceResult.Failure("invalid_owner_user", "Owner user does not exist or is inactive.");
            }
        }

        return ServiceResult.Success();
    }

    private async Task<CommissionQuote> ResolveCommissionAsync(CreateOrderRequestDto request, int? excludeOrderId = null)
    {
        if (request.OrderType == "companion")
        {
            var primaryMemberId = request.Members.FirstOrDefault()?.UserId;
            if (primaryMemberId is null || request.ServiceQuantity <= 0)
            {
                return new CommissionQuote(0m, 0m);
            }

            var monthStart = new DateOnly(request.OrderDate.Year, request.OrderDate.Month, 1);
            var nextMonth = monthStart.AddMonths(1);
            var completedHours = await _db.OrderMembers.AsNoTracking()
                .Where(x => x.UserId == primaryMemberId.Value
                    && x.Order.Status == "completed"
                    && x.Order.OrderType == "companion"
                    && x.Order.OrderDate >= monthStart
                    && x.Order.OrderDate < nextMonth
                    && (!excludeOrderId.HasValue || x.OrderId != excludeOrderId.Value))
                .SumAsync(x => x.Order.ServiceQuantity);
            var commissionAmount = CalculateCompanionTierCommission(
                request.Amount,
                request.ServiceQuantity,
                completedHours);
            var effectiveRate = request.Amount > 0
                ? decimal.Round(commissionAmount / request.Amount, 4, MidpointRounding.AwayFromZero)
                : 0m;
            return new CommissionQuote(effectiveRate, commissionAmount);
        }

        var manualAmount = request.CommissionAmount ?? 0m;
        var manualRate = request.Amount > 0
            ? decimal.Round(manualAmount / request.Amount, 4, MidpointRounding.AwayFromZero)
            : 0m;
        return new CommissionQuote(manualRate, manualAmount);
    }

    private Task<CommissionQuote> ResolveCommissionAsync(UpdateOrderRequestDto request, int? excludeOrderId = null)
    {
        return ResolveCommissionAsync(
            new CreateOrderRequestDto
            {
                OrderNo = request.OrderNo,
                OrderType = request.OrderType,
                OrderDate = request.OrderDate,
                OwnerUserId = request.OwnerUserId,
                Amount = request.Amount,
                ServiceQuantity = request.ServiceQuantity,
                CommissionRate = request.CommissionRate,
                CommissionAmount = request.CommissionAmount,
                Status = request.Status,
                CustomerPaymentStatus = request.CustomerPaymentStatus,
                Remark = request.Remark,
                Members = request.Members
            },
            excludeOrderId);
    }

    private static decimal CalculateCompanionTierCommission(decimal amount, decimal hours, decimal completedHours)
    {
        if (amount <= 0 || hours <= 0)
        {
            return 0m;
        }

        var hourlyAmount = amount / hours;
        var remainingHours = hours;
        var cursor = completedHours;
        var commission = 0m;

        while (remainingHours > 0)
        {
            var rate = CompanionCommissionRate(cursor);
            var nextBoundary = cursor < 15m
                ? 15m
                : cursor < 30m
                    ? 30m
                    : decimal.MaxValue;
            var tierHours = nextBoundary == decimal.MaxValue
                ? remainingHours
                : Math.Min(remainingHours, nextBoundary - cursor);

            commission += hourlyAmount * tierHours * rate;
            remainingHours -= tierHours;
            cursor += tierHours;
        }

        return decimal.Round(commission, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CompanionCommissionRate(decimal completedHours)
    {
        if (completedHours < 15m)
        {
            return 0.25m;
        }

        if (completedHours < 30m)
        {
            return 0.20m;
        }

        return 0.10m;
    }

    private IQueryable<Order> GetOrderWithRelations(int orderId)
    {
        return _db.Orders.AsNoTracking()
            .Include(x => x.OwnerUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Where(x => x.Id == orderId);
    }

    private async Task<ServiceResult> ValidateRequiredAttachmentsAsync(
        int orderId,
        string status,
        string customerPaymentStatus)
    {
        if (status == "disputed" || customerPaymentStatus is "paid" or "partial")
        {
            return await _attachmentRequirementService.RequireAsync(
                "orders",
                orderId,
                "attachment_required",
                "Attachment is required for disputed or paid orders.");
        }

        return ServiceResult.Success();
    }

    private static Dictionary<string, string[]> ValidateOrderValues(
        string orderType,
        string status,
        string customerPaymentStatus,
        IEnumerable<string> memberRoles)
    {
        var errors = new Dictionary<string, string[]>();

        if (!DomainValues.IsOrderType(orderType))
        {
            errors["orderType"] = ["OrderType must be boosting, farming, companion, or prepaid."];
        }

        if (!DomainValues.IsOrderStatus(status))
        {
            errors["status"] = ["Status must be draft, completed, cancelled, or disputed."];
        }

        if (!DomainValues.IsCustomerPaymentStatus(customerPaymentStatus))
        {
            errors["customerPaymentStatus"] = ["CustomerPaymentStatus must be unpaid, partial, paid, or refunded."];
        }

        var invalidRoles = memberRoles
            .Where(x => !DomainValues.IsOrderMemberRole(x))
            .Distinct()
            .ToList();
        if (invalidRoles.Count > 0)
        {
            errors["members.role"] = [$"Invalid role: {string.Join(", ", invalidRoles)}."];
        }

        return errors;
    }

    private static ServiceResult<T> ToGenericResult<T>(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            return ServiceResult<T>.Validation(result.ValidationErrors);
        }

        if (result.NotFound)
        {
            return ServiceResult<T>.Missing();
        }

        return ServiceResult<T>.Failure(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }

    private sealed record CommissionQuote(decimal Rate, decimal Amount);
}
