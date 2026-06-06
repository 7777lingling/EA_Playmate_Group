using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Data;

public sealed class EAPlaymateGroupDbContext : DbContext
{
    public EAPlaymateGroupDbContext(DbContextOptions<EAPlaymateGroupDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderMember> OrderMembers => Set<OrderMember>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderMember(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigureAuditLog(modelBuilder);
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
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        entity.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(50).IsRequired();
        entity.Property(x => x.TargetId).HasColumnName("target_id");
        entity.Property(x => x.TargetUuid).HasColumnName("target_uuid");
        entity.Property(x => x.BeforeJson).HasColumnName("before_json");
        entity.Property(x => x.AfterJson).HasColumnName("after_json");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasIndex(x => new { x.TargetType, x.TargetId, x.CreatedAt })
            .HasDatabaseName("IX_audit_logs_target");

        entity.HasOne(x => x.User)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_audit_logs_user")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
