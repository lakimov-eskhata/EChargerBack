using Application.Common.Interfaces;
using Domain.Entities.Station;
using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    // // Таблицы
    // public DbSet<CompanyEntity> Companies { get; set; } = null!;
    // public DbSet<CountryEntity> Countries { get; set; } = null!;
    // public DbSet<RegionEntity> Regions { get; set; } = null!;
    // public DbSet<CityEntity> Cities { get; set; } = null!;
    //
    // public DbSet<StationEntity> Stations { get; set; } = null!;
    // public DbSet<ChargePointEntity> ChargePoints { get; set; } = null!;
    // public DbSet<ConnectorEntity> Connectors { get; set; } = null!;
    //
    // public DbSet<UserEntity> Users { get; set; } = null!;
    // public DbSet<AdminUserEntity> AdminUsers { get; set; } = null!;
    // public DbSet<ClientUserEntity> ClientUsers { get; set; } = null!;
    //
    // public DbSet<RoleEntity> Roles { get; set; } = null!;
    // public DbSet<PermissionEntity> Permissions { get; set; } = null!;
    //
    // public DbSet<StationUsersEntity> StationUsers { get; set; } = null!;
    // public DbSet<HistoryChargesEntity> HistoryCharges { get; set; } = null!;
    // public DbSet<DynamicTrafficPriceEntity> DynamicTrafficPrices { get; set; } = null!;

    public DbSet<StationEntity> Stations { get; set; }
    public DbSet<ChargePointEntity> ChargePoints { get; set; }
    public DbSet<ConnectorEntity> Connectors { get; set; }
    public DbSet<MeterValueEntity> MeterValues { get; set; }
    public DbSet<TransactionEntity> Transactions { get; set; }

    // User management
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<CompanyEntity> Companies { get; set; }
    public DbSet<RoleEntity> Roles { get; set; }
    public DbSet<PermissionEntity> Permissions { get; set; }
    public DbSet<UserRoleEntity> UserRoles { get; set; }
    public DbSet<RolePermissionEntity> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Station configuration
        modelBuilder.Entity<StationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Address)
                .HasMaxLength(500);

            entity.Property(e => e.City)
                .HasMaxLength(100);

            entity.Property(e => e.Region)
                .HasMaxLength(100);

            entity.Property(e => e.PostalCode)
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.Latitude)
                .HasPrecision(10, 8);

            entity.Property(e => e.Longitude)
                .HasPrecision(11, 8);

            // Relationships
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.ChargePoints)
                .WithOne(e => e.Station)
                .HasForeignKey(e => e.StationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ChargePoint configuration
        modelBuilder.Entity<ChargePointEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChargePointId).IsUnique();
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ProtocolVersion);

            entity.Property(e => e.ChargePointId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Name)
                .HasMaxLength(200);

            entity.Property(e => e.ProtocolVersion)
                .HasMaxLength(10)
                .HasDefaultValue("1.6");

            entity.Property(e => e.Vendor)
                .HasMaxLength(100);

            entity.Property(e => e.Model)
                .HasMaxLength(100);

            entity.Property(e => e.SerialNumber)
                .HasMaxLength(100);

            entity.Property(e => e.FirmwareVersion)
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Offline");

            entity.Property(e => e.MeterType)
                .HasMaxLength(50);

            entity.Property(e => e.MeterSerialNumber)
                .HasMaxLength(100);

            entity.Property(e => e.Iccid)
                .HasMaxLength(50);

            entity.Property(e => e.Imsi)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            // Relationships
            entity.HasOne(e => e.Station)
                .WithMany(e => e.ChargePoints)
                .HasForeignKey(e => e.StationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Connectors)
                .WithOne(e => e.ChargePoint)
                .HasForeignKey(e => e.ChargePointId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Transactions)
                .WithOne(e => e.ChargePoint)
                .HasForeignKey(e => e.ChargePointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Connector configuration
        modelBuilder.Entity<ConnectorEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Composite unique constraint: ChargePointId + ConnectorId
            entity.HasIndex(e => new { e.ChargePointId, e.ConnectorId })
                .IsUnique();

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TransactionId);

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Available");

            entity.Property(e => e.ErrorCode)
                .HasMaxLength(50);

            entity.Property(e => e.Info)
                .HasMaxLength(500);

            // Relationships
            entity.HasOne(e => e.ChargePoint)
                .WithMany(e => e.Connectors)
                .HasForeignKey(e => e.ChargePointId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ActiveTransaction)
                .WithOne()
                .HasForeignKey<ConnectorEntity>(e => e.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Transaction configuration
        modelBuilder.Entity<TransactionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.IdTag);
            entity.HasIndex(e => e.ChargePointId);
            entity.HasIndex(e => e.StartTimestamp);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.TransactionId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.IdTag)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Reason)
                .HasMaxLength(200);

            entity.Property(e => e.ParentIdTag)
                .HasMaxLength(100);
            
            // Relationships
            entity.HasOne(e => e.ChargePoint)
                .WithMany(e => e.Transactions)
                .HasForeignKey(e => e.ChargePointId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.MeterValues)
                .WithOne(e => e.Transaction)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MeterValue configuration
        modelBuilder.Entity<MeterValueEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.TransactionId, e.Measurand });

            entity.Property(e => e.Context)
                .HasMaxLength(50);

            entity.Property(e => e.Format)
                .HasMaxLength(50);

            entity.Property(e => e.Measurand)
                .HasMaxLength(100);

            entity.Property(e => e.Phase)
                .HasMaxLength(10);

            entity.Property(e => e.Location)
                .HasMaxLength(50);

            entity.Property(e => e.Unit)
                .HasMaxLength(20);

            // Relationships
            entity.HasOne(e => e.Transaction)
                .WithMany(e => e.MeterValues)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Company configuration
        modelBuilder.Entity<CompanyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ContactEmail);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.ContactEmail)
                .HasMaxLength(200);

            entity.Property(e => e.ContactPhone)
                .HasMaxLength(50);

            entity.Property(e => e.Address)
                .HasMaxLength(500);

            entity.Property(e => e.Balance)
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            // Relationships
            entity.HasMany(e => e.Users)
                .WithOne(e => e.Company)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // User configuration
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CompanyId);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.FirstName)
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .HasMaxLength(100);

            entity.Property(e => e.Phone)
                .HasMaxLength(50);

            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.Property(e => e.PasswordSalt)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            // Relationships
            entity.HasOne(e => e.Company)
                .WithMany(e => e.Users)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Role configuration
        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Permission configuration
        modelBuilder.Entity<PermissionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Resource, e.Action }).IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Resource)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UserRole configuration (many-to-many)
        modelBuilder.Entity<UserRoleEntity>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne(e => e.User)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RolePermission configuration (many-to-many)
        modelBuilder.Entity<RolePermissionEntity>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasOne(e => e.Role)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}