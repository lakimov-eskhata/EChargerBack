using Domain.Entities.Station;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    
    // // Таблицы
    // public DbSet<CompanyEntity> Companies { get; set; } 
    // public DbSet<CountryEntity> Countries { get; set; } 
    // public DbSet<RegionEntity> Regions { get; set; } 
    // public DbSet<CityEntity> Cities { get; set; } 
    //
    // public DbSet<StationEntity> Stations { get; set; }
    // public DbSet<ChargePointEntity> ChargePoints { get; set; }
    // public DbSet<ConnectorEntity> Connectors { get; set; } 
    //
    // public DbSet<UserEntity> Users { get; set; } 
    // public DbSet<AdminUserEntity> AdminUsers { get; set; }
    // public DbSet<ClientUserEntity> ClientUsers { get; set; }
    //
    // public DbSet<RoleEntity> Roles { get; set; } 
    // public DbSet<PermissionEntity> Permissions { get; set; } 
    //
    // public DbSet<StationUsersEntity> StationUsers { get; set; } 
    // public DbSet<HistoryChargesEntity> HistoryCharges { get; set; }
    // public DbSet<DynamicTrafficPriceEntity> DynamicTrafficPrices { get; set; }
    
    public DbSet<ChargePointEntity> ChargePoints { get; set; }
    public DbSet<ConnectorEntity> Connectors { get; set; }
    public DbSet<MeterValueEntity> MeterValues { get; set; }
    public DbSet<TransactionEntity> Transactions { get; set; }
    
    EntityEntry Entry(object entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    DatabaseFacade Database { get; }
}