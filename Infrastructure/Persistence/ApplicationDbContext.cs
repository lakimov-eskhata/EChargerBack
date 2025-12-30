using Application.Common.Interfaces;
using Domain.Entities.Station;
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

    public DbSet<ChargePointEntity> ChargePoints { get; set; }
    public DbSet<ConnectorEntity> Connectors { get; set; }
    public DbSet<MeterValueEntity> MeterValues { get; set; }
    public DbSet<TransactionEntity> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}