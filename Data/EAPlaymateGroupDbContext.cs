using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Data;

public sealed class EAPlaymateGroupDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EAPlaymateGroupDbContext(
        DbContextOptions<EAPlaymateGroupDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<LoginUser> LoginUsers => Set<LoginUser>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderMember> OrderMembers => Set<OrderMember>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<GiftRecord> GiftRecords => Set<GiftRecord>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentMember> DepartmentMembers => Set<DepartmentMember>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Organization> Organizations => Set<Organization>();

    private int CurrentOrganizationId =>
        _httpContextAccessor.HttpContext?.Session.GetInt32(Services.AuthService.SessionOrganizationId) ?? 0;

    private bool IsSystemAdmin =>
        _httpContextAccessor.HttpContext?.Session.GetString(Services.AuthService.SessionSystemRole) == "admin";

    private bool IsMember =>
        _httpContextAccessor.HttpContext?.Session.GetString(Services.AuthService.SessionSystemRole) == "viewer";

    private int CurrentMemberUserId =>
        _httpContextAccessor.HttpContext?.Session.GetInt32(Services.AuthService.SessionMemberUserId) ?? 0;

    private int CurrentLoginUserId =>
        _httpContextAccessor.HttpContext?.Session.GetInt32(Services.AuthService.SessionUserId) ?? 0;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureLoginUser(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderMember(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureServiceItem(modelBuilder);
        ConfigureGiftRecord(modelBuilder);
        ConfigureDepartment(modelBuilder);
        ConfigureDepartmentMember(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureOrganization(modelBuilder);
        ConfigureOrganizationFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditActors();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampAuditActors();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampAuditActors()
    {
        var loginUserId = _httpContextAccessor.HttpContext?.Session.GetInt32(EAPlaymateGroup.Services.AuthService.SessionUserId);
        var organizationId = CurrentOrganizationId;
        if (organizationId > 0)
        {
            foreach (var entry in ChangeTracker.Entries<IOrganizationScoped>()
                         .Where(x => x.State == EntityState.Added && x.Entity.OrganizationId == 0))
            {
                entry.Entity.OrganizationId = organizationId;
            }
        }

        var missingOrganizationEntries = ChangeTracker.Entries<IOrganizationScoped>()
            .Where(x =>
                (x.State == EntityState.Added || x.State == EntityState.Modified) &&
                x.Entity.OrganizationId <= 0)
            .Select(x => x.Metadata.ClrType.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (missingOrganizationEntries.Count > 0)
        {
            throw new InvalidOperationException(
                $"OrganizationId is required for: {string.Join(", ", missingOrganizationEntries)}.");
        }

        if (!loginUserId.HasValue)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<AuditLog>()
                     .Where(x => x.State == EntityState.Added && !x.Entity.LoginUserId.HasValue))
        {
            entry.Entity.LoginUserId = loginUserId.Value;
            entry.Entity.IpAddress ??= _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        entity.ToTable("users", "dbo", table =>
        {
            table.HasCheckConstraint("CK_users_system_role", "[system_role] IN (N'admin', N'staff', N'viewer')");
        });

        entity.HasKey(x => x.Id).HasName("PK_users");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.Uuid).HasColumnName("uuid").HasDefaultValueSql("NEWID()");
        entity.Property(x => x.Nickname).HasColumnName("nickname").HasMaxLength(50).IsRequired();
        entity.Property(x => x.DiscordId).HasColumnName("discord_id").HasMaxLength(50);
        entity.Property(x => x.DiscordName).HasColumnName("discord_name").HasMaxLength(100);
        entity.Property(x => x.BankAccount).HasColumnName("bank_account").HasMaxLength(200);
        entity.Property(x => x.LoginAccount).HasColumnName("login_account").HasMaxLength(50);
        entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
        entity.Property(x => x.SystemRole).HasColumnName("system_role").HasMaxLength(20).HasDefaultValue("staff").IsRequired();
        entity.Property(x => x.IsPlayer).HasColumnName("is_player").HasDefaultValue(true);
        entity.Property(x => x.IsBoss).HasColumnName("is_boss").HasDefaultValue(false);
        entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(x => x.LeftAt).HasColumnName("left_at");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");

        entity.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UQ_users_uuid");
        entity.HasIndex(x => x.Nickname).IsUnique().HasDatabaseName("UQ_users_nickname");
        entity.HasIndex(x => x.LoginAccount)
            .IsUnique()
            .HasFilter("[login_account] IS NOT NULL")
            .HasDatabaseName("UQ_users_login_account");
        entity.HasIndex(x => x.DiscordId)
            .IsUnique()
            .HasFilter("[discord_id] IS NOT NULL")
            .HasDatabaseName("UQ_users_discord_id");
    }

    private static void ConfigureLoginUser(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LoginUser>();

        entity.ToTable("login_users", "dbo", table =>
        {
            table.HasCheckConstraint("CK_login_users_system_role", "[system_role] IN (N'admin', N'staff', N'viewer')");
        });

        entity.HasKey(x => x.Id).HasName("PK_login_users");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.Uuid).HasColumnName("uuid").HasDefaultValueSql("NEWID()");
        entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(50).IsRequired();
        entity.Property(x => x.LoginAccount).HasColumnName("login_account").HasMaxLength(50).IsRequired();
        entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        entity.Property(x => x.DiscordId).HasColumnName("discord_id").HasMaxLength(50);
        entity.Property(x => x.DiscordName).HasColumnName("discord_name").HasMaxLength(100);
        entity.Property(x => x.SystemRole).HasColumnName("system_role").HasMaxLength(20).HasDefaultValue("staff").IsRequired();
        entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");

        entity.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UQ_login_users_uuid");
        entity.HasIndex(x => x.LoginAccount).IsUnique().HasDatabaseName("UQ_login_users_login_account");
        entity.HasIndex(x => x.DiscordId)
            .IsUnique()
            .HasFilter("[discord_id] IS NOT NULL")
            .HasDatabaseName("UQ_login_users_discord_id");
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Order>();

        entity.ToTable("orders", "dbo", table =>
        {
            table.HasCheckConstraint("CK_orders_amount", "[amount] >= 0");
            table.HasCheckConstraint("CK_orders_commission_rate", "[commission_rate] >= 0 AND [commission_rate] <= 1");
            table.HasCheckConstraint("CK_orders_commission_amount", "[commission_amount] >= 0");
            table.HasCheckConstraint("CK_orders_status", "[status] IN (N'draft', N'completed', N'cancelled', N'disputed')");
            table.HasCheckConstraint("CK_orders_customer_payment_status", "[customer_payment_status] IN (N'unpaid', N'partial', N'paid', N'refunded')");
        });

        entity.HasKey(x => x.Id).HasName("PK_orders");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.Uuid).HasColumnName("uuid").HasDefaultValueSql("NEWID()");
        entity.Property(x => x.OrderNo).HasColumnName("order_no").HasMaxLength(30);
        entity.Property(x => x.OrderDate).HasColumnName("order_date");
        entity.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
        entity.Property(x => x.Amount).HasColumnName("amount").HasPrecision(12, 2);
        entity.Property(x => x.CommissionRate).HasColumnName("commission_rate").HasPrecision(6, 4).HasDefaultValue(0.1000m);
        entity.Property(x => x.CommissionAmount).HasColumnName("commission_amount").HasPrecision(12, 2);
        entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("completed").IsRequired();
        entity.Property(x => x.CustomerPaymentStatus).HasColumnName("customer_payment_status").HasMaxLength(20).HasDefaultValue("unpaid").IsRequired();
        entity.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UQ_orders_uuid");
        entity.HasIndex(x => x.OrderNo).IsUnique().HasDatabaseName("UQ_orders_order_no");
        entity.HasIndex(x => new { x.OrderDate, x.Status })
            .IncludeProperties(x => new { x.Amount, x.CommissionAmount, x.CustomerPaymentStatus })
            .HasDatabaseName("IX_orders_order_date_status");
        entity.HasIndex(x => new { x.CustomerPaymentStatus, x.Status, x.OrderDate })
            .HasDatabaseName("IX_orders_customer_payment_status");

        entity.HasOne(x => x.OwnerUser)
            .WithMany(x => x.OwnedOrders)
            .HasForeignKey(x => x.OwnerUserId)
            .HasConstraintName("FK_orders_owner_user")
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureOrderMember(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OrderMember>();

        entity.ToTable("order_members", "dbo", table =>
        {
            table.HasCheckConstraint("CK_order_members_role", "[role] IN (N'player', N'leader', N'trainer', N'bonus')");
            table.HasCheckConstraint("CK_order_members_share_amount", "[share_amount] >= 0");
        });

        entity.HasKey(x => x.Id).HasName("PK_order_members");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.OrderId).HasColumnName("order_id");
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("player").IsRequired();
        entity.Property(x => x.ShareAmount).HasColumnName("share_amount").HasPrecision(12, 2);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasIndex(x => new { x.OrderId, x.UserId, x.Role })
            .IsUnique()
            .HasDatabaseName("UQ_order_members_order_user_role");
        entity.HasIndex(x => new { x.UserId, x.OrderId })
            .IncludeProperties(x => new { x.ShareAmount, x.Role })
            .HasDatabaseName("IX_order_members_user_order");

        entity.HasOne(x => x.Order)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.OrderId)
            .HasConstraintName("FK_order_members_order")
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.User)
            .WithMany(x => x.OrderMembers)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_order_members_user")
            .OnDelete(DeleteBehavior.NoAction);

    }

    private static void ConfigurePayment(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Payment>();

        entity.ToTable("payments", "dbo", table =>
        {
            table.HasCheckConstraint("CK_payments_pay_month", "[pay_month] LIKE '[1-2][0-9][0-9][0-9]-[0-1][0-9]'");
            table.HasCheckConstraint("CK_payments_expected_amount", "[expected_amount] >= 0");
            table.HasCheckConstraint("CK_payments_actual_amount", "[actual_amount] IS NULL OR [actual_amount] >= 0");
            table.HasCheckConstraint("CK_payments_payment_status", "[payment_status] IN (N'pending', N'paid', N'cancelled')");
        });

        entity.HasKey(x => x.Id).HasName("PK_payments");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.Uuid).HasColumnName("uuid").HasDefaultValueSql("NEWID()");
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.PayMonth).HasColumnName("pay_month").HasMaxLength(7).IsFixedLength().IsRequired();
        entity.Property(x => x.ExpectedAmount).HasColumnName("expected_amount").HasPrecision(12, 2);
        entity.Property(x => x.ActualAmount).HasColumnName("actual_amount").HasPrecision(12, 2);
        entity.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(20).HasDefaultValue("pending").IsRequired();
        entity.Property(x => x.SnapshotJson).HasColumnName("snapshot_json");
        entity.Property(x => x.PaidAt).HasColumnName("paid_at");
        entity.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UQ_payments_uuid");
        entity.HasIndex(x => new { x.UserId, x.PayMonth }).IsUnique().HasDatabaseName("UQ_payments_user_month");
        entity.HasIndex(x => new { x.PayMonth, x.PaymentStatus }).HasDatabaseName("IX_payments_pay_month");

        entity.HasOne(x => x.User)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_payments_user")
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLog>();

        entity.ToTable("audit_logs", "dbo");

        entity.HasKey(x => x.Id).HasName("PK_audit_logs");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.LoginUserId).HasColumnName("login_user_id");
        entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        entity.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(50).IsRequired();
        entity.Property(x => x.TargetId).HasColumnName("target_id");
        entity.Property(x => x.TargetUuid).HasColumnName("target_uuid");
        entity.Property(x => x.BeforeJson).HasColumnName("before_json");
        entity.Property(x => x.AfterJson).HasColumnName("after_json");
        entity.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasIndex(x => new { x.TargetType, x.TargetId, x.CreatedAt })
            .HasDatabaseName("IX_audit_logs_target");
        entity.HasIndex(x => new { x.LoginUserId, x.CreatedAt })
            .HasDatabaseName("IX_audit_logs_login_user");

        entity.HasOne(x => x.User)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_audit_logs_user")
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.LoginUser)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.LoginUserId)
            .HasConstraintName("FK_audit_logs_login_user")
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureServiceItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ServiceItem>();

        entity.ToTable("service_items", "dbo", table =>
        {
            table.HasCheckConstraint("CK_service_items_default_price", "[default_price] IS NULL OR [default_price] >= 0");
            table.HasCheckConstraint("CK_service_items_category", "[category] IN (N'boost', N'grind', N'play', N'gift', N'deposit_bonus', N'other')");
        });

        entity.HasKey(x => x.Id).HasName("PK_service_items");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.Uuid).HasColumnName("uuid").HasDefaultValueSql("NEWID()");
        entity.Property(x => x.SeedKey).HasColumnName("seed_key").HasMaxLength(80).IsRequired();
        entity.Property(x => x.Category).HasColumnName("category").HasMaxLength(30).IsRequired();
        entity.Property(x => x.Subcategory).HasColumnName("subcategory").HasMaxLength(50);
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        entity.Property(x => x.UnitType).HasColumnName("unit_type").HasMaxLength(30).HasDefaultValue("custom").IsRequired();
        entity.Property(x => x.DefaultPrice).HasColumnName("default_price").HasPrecision(12, 2);
        entity.Property(x => x.PriceNote).HasColumnName("price_note").HasMaxLength(200);
        entity.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(1000);
        entity.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UQ_service_items_uuid");
        entity.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_service_items_seed_key");
        entity.HasIndex(x => new { x.Category, x.SortOrder }).HasDatabaseName("IX_service_items_category_sort");
    }

    private static void ConfigureGiftRecord(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<GiftRecord>();

        entity.ToTable("gift_records", "dbo", table =>
        {
            table.HasCheckConstraint("CK_gift_records_amount", "[amount] > 0");
            table.HasCheckConstraint("CK_gift_records_quantity", "[quantity] > 0");
            table.HasCheckConstraint("CK_gift_records_customer_payment_status", "[customer_payment_status] IN (N'unpaid', N'partial', N'paid', N'refunded')");
            table.HasCheckConstraint("CK_gift_records_status", "[status] IN (N'completed', N'cancelled')");
        });

        entity.HasKey(x => x.Id).HasName("PK_gift_records");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.Uuid).HasColumnName("uuid").HasDefaultValueSql("NEWID()");
        entity.Property(x => x.GiftDate).HasColumnName("gift_date");
        entity.Property(x => x.BossUserId).HasColumnName("boss_user_id");
        entity.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id");
        entity.Property(x => x.ServiceItemId).HasColumnName("service_item_id");
        entity.Property(x => x.GiftName).HasColumnName("gift_name").HasMaxLength(100).IsRequired();
        entity.Property(x => x.Amount).HasColumnName("amount").HasPrecision(12, 2);
        entity.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(12, 2).HasDefaultValue(1m);
        entity.Property(x => x.CustomerPaymentStatus).HasColumnName("customer_payment_status").HasMaxLength(20).HasDefaultValue("unpaid").IsRequired();
        entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("completed").IsRequired();
        entity.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UQ_gift_records_uuid");
        entity.HasIndex(x => new { x.GiftDate, x.Status }).HasDatabaseName("IX_gift_records_date_status");
        entity.HasIndex(x => new { x.BossUserId, x.GiftDate }).HasDatabaseName("IX_gift_records_boss_date");
        entity.HasIndex(x => new { x.RecipientUserId, x.GiftDate }).HasDatabaseName("IX_gift_records_recipient_date");

        entity.HasOne(x => x.BossUser)
            .WithMany(x => x.SentGiftRecords)
            .HasForeignKey(x => x.BossUserId)
            .HasConstraintName("FK_gift_records_boss_user")
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.RecipientUser)
            .WithMany(x => x.ReceivedGiftRecords)
            .HasForeignKey(x => x.RecipientUserId)
            .HasConstraintName("FK_gift_records_recipient_user")
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.ServiceItem)
            .WithMany()
            .HasForeignKey(x => x.ServiceItemId)
            .HasConstraintName("FK_gift_records_service_item")
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureDepartment(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Department>();

        entity.ToTable("departments", "dbo");
        entity.HasKey(x => x.Id).HasName("PK_departments");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.Uuid).HasColumnName("uuid").HasDefaultValueSql("NEWID()");
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        entity.Property(x => x.EnglishName).HasColumnName("english_name").HasMaxLength(80);
        entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UQ_departments_uuid");
        entity.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_departments_name");
        entity.HasIndex(x => new { x.SortOrder, x.Name }).HasDatabaseName("IX_departments_sort");
    }

    private static void ConfigureDepartmentMember(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DepartmentMember>();

        entity.ToTable("department_members", "dbo");
        entity.HasKey(x => x.Id).HasName("PK_department_members");

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.OrganizationId).HasColumnName("organization_id");
        entity.Property(x => x.DepartmentId).HasColumnName("department_id");
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.PositionTitle).HasColumnName("position_title").HasMaxLength(80);
        entity.Property(x => x.IsManager).HasColumnName("is_manager").HasDefaultValue(false);
        entity.Property(x => x.JoinedAt).HasColumnName("joined_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.LeftAt).HasColumnName("left_at");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasIndex(x => new { x.DepartmentId, x.UserId })
            .HasFilter("[left_at] IS NULL")
            .IsUnique()
            .HasDatabaseName("UQ_department_members_active");
        entity.HasIndex(x => new { x.UserId, x.DepartmentId }).HasDatabaseName("IX_department_members_user");

        entity.HasOne(x => x.Department)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_department_members_department")
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.User)
            .WithMany(x => x.DepartmentMembers)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_department_members_user")
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RolePermission>();

        entity.ToTable("role_permissions", "dbo");
        entity.HasKey(x => x.Id).HasName("PK_role_permissions");
        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.SystemRole).HasColumnName("system_role").HasMaxLength(20).IsRequired();
        entity.Property(x => x.PermissionCode).HasColumnName("permission_code").HasMaxLength(80).IsRequired();
        entity.Property(x => x.IsAllowed).HasColumnName("is_allowed").HasDefaultValue(false);
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.HasIndex(x => new { x.SystemRole, x.PermissionCode })
            .IsUnique()
            .HasDatabaseName("UQ_role_permissions_role_code");
    }

    private static void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Organization>();
        entity.ToTable("organizations", "dbo");
        entity.HasKey(x => x.Id).HasName("PK_organizations");
        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_organizations_name");
    }

    private void ConfigureOrganizationFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginUser>().HasQueryFilter(x =>
            IsSystemAdmin ||
            (!IsMember && CurrentOrganizationId > 0 && x.OrganizationId == CurrentOrganizationId) ||
            (IsMember && CurrentLoginUserId > 0 && x.Id == CurrentLoginUserId));
        modelBuilder.Entity<User>().HasQueryFilter(x =>
            IsSystemAdmin ||
            (!IsMember && CurrentOrganizationId > 0 && x.OrganizationId == CurrentOrganizationId) ||
            (IsMember && CurrentMemberUserId > 0 && x.Id == CurrentMemberUserId));
        modelBuilder.Entity<Order>().HasQueryFilter(x =>
            IsSystemAdmin ||
            (CurrentOrganizationId > 0 &&
             x.OrganizationId == CurrentOrganizationId &&
             (!IsMember ||
              (CurrentMemberUserId > 0 &&
               (x.OwnerUserId == CurrentMemberUserId ||
                x.Members.Any(m => m.UserId == CurrentMemberUserId))))));
        modelBuilder.Entity<OrderMember>().HasQueryFilter(x =>
            IsSystemAdmin ||
            (CurrentOrganizationId > 0 &&
             x.OrganizationId == CurrentOrganizationId &&
             (!IsMember ||
              (CurrentMemberUserId > 0 &&
               x.UserId == CurrentMemberUserId))));
        modelBuilder.Entity<Payment>().HasQueryFilter(x =>
            IsSystemAdmin ||
            (CurrentOrganizationId > 0 &&
             x.OrganizationId == CurrentOrganizationId &&
             (!IsMember || (CurrentMemberUserId > 0 && x.UserId == CurrentMemberUserId))));
        modelBuilder.Entity<GiftRecord>().HasQueryFilter(x =>
            IsSystemAdmin ||
            (CurrentOrganizationId > 0 &&
             x.OrganizationId == CurrentOrganizationId &&
             (!IsMember ||
              (CurrentMemberUserId > 0 &&
               (x.BossUserId == CurrentMemberUserId ||
                x.RecipientUserId == CurrentMemberUserId)))));
        modelBuilder.Entity<ServiceItem>().HasQueryFilter(x =>
            IsSystemAdmin || (CurrentOrganizationId > 0 && x.OrganizationId == CurrentOrganizationId));
        modelBuilder.Entity<Department>().HasQueryFilter(x =>
            IsSystemAdmin || (CurrentOrganizationId > 0 && x.OrganizationId == CurrentOrganizationId));
        modelBuilder.Entity<DepartmentMember>().HasQueryFilter(x =>
            IsSystemAdmin ||
            (!IsMember &&
             CurrentOrganizationId > 0 &&
             x.OrganizationId == CurrentOrganizationId));
        modelBuilder.Entity<AuditLog>().HasQueryFilter(x =>
            IsSystemAdmin || (CurrentOrganizationId > 0 && x.OrganizationId == CurrentOrganizationId));
    }
}
