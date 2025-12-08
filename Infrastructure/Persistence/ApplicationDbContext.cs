using System.Reflection;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Таблицы
    public DbSet<CompanyEntity> Companies { get; set; } = null!;
    public DbSet<CountryEntity> Countries { get; set; } = null!;
    public DbSet<RegionEntity> Regions { get; set; } = null!;
    public DbSet<CityEntity> Cities { get; set; } = null!;

    public DbSet<StationEntity> Stations { get; set; } = null!;
    public DbSet<ChargePointEntity> ChargePoints { get; set; } = null!;
    public DbSet<ConnectorEntity> Connectors { get; set; } = null!;

    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<AdminUserEntity> AdminUsers { get; set; } = null!;
    public DbSet<ClientUserEntity> ClientUsers { get; set; } = null!;

    public DbSet<RoleEntity> Roles { get; set; } = null!;
    public DbSet<PermissionEntity> Permissions { get; set; } = null!;

    public DbSet<StationUsersEntity> StationUsers { get; set; } = null!;
    public DbSet<HistoryChargesEntity> HistoryCharges { get; set; } = null!;
    public DbSet<DynamicTrafficPriceEntity> DynamicTrafficPrices { get; set; } = null!;

    // Конфигурации
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Автоматически проставляем даты
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}