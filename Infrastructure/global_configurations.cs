// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
// using Domain.Entities;
//
// namespace Infrastructure;
//
// // Компания
// public class CompanyConfiguration : IEntityTypeConfiguration<CompanyEntity>
// {
//     public void Configure(EntityTypeBuilder<CompanyEntity> builder)
//     {
//         builder.Property(c => c.Name)
//             .HasMaxLength(200)
//             .IsRequired();
//
//         builder.Property(c => c.INN)
//             .HasMaxLength(50);
//
//         builder.Property(c => c.Address)
//             .HasMaxLength(300);
//
//         builder.HasMany(c => c.Stations)
//             .WithOne(s => s.Company)
//             .HasForeignKey(s => s.CompanyId);
//     }
// }
//
// // Страна
// public class CountryConfiguration : IEntityTypeConfiguration<CountryEntity>
// {
//     public void Configure(EntityTypeBuilder<CountryEntity> builder)
//     {
//         builder.Property(c => c.Name)
//             .HasMaxLength(150)
//             .IsRequired();
//
//         builder.Property(c => c.IsoCode)
//             .HasMaxLength(10);
//
//         builder.HasMany(c => c.Regions)
//             .WithOne(r => r.Country)
//             .HasForeignKey(r => r.CountryId);
//     }
// }
//
// // Регион
// public class RegionConfiguration : IEntityTypeConfiguration<RegionEntity>
// {
//     public void Configure(EntityTypeBuilder<RegionEntity> builder)
//     {
//         builder.Property(r => r.Name)
//             .HasMaxLength(150)
//             .IsRequired();
//
//         builder.HasOne(r => r.Country)
//             .WithMany(c => c.Regions)
//             .HasForeignKey(r => r.CountryId);
//     }
// }
//
// // Город
// public class CityConfiguration : IEntityTypeConfiguration<CityEntity>
// {
//     public void Configure(EntityTypeBuilder<CityEntity> builder)
//     {
//         builder.Property(c => c.Name)
//             .HasMaxLength(150)
//             .IsRequired();
//
//         builder.HasOne(c => c.Region)
//             .WithMany(r => r.Cities)
//             .HasForeignKey(c => c.RegionId);
//     }
// }
//
// // Станция (заправка)
// public class StationConfiguration : IEntityTypeConfiguration<StationEntity>
// {
//     public void Configure(EntityTypeBuilder<StationEntity> builder)
//     {
//         builder.Property(s => s.Name)
//             .HasMaxLength(200)
//             .IsRequired();
//
//         builder.Property(s => s.Address)
//             .HasMaxLength(300);
//
//         builder.HasOne(s => s.City)
//             .WithMany(c => c.Stations)
//             .HasForeignKey(s => s.CityId);
//
//         builder.HasOne(s => s.Company)
//             .WithMany(c => c.Stations)
//             .HasForeignKey(s => s.CompanyId);
//     }
// }
//
// // Зарядная точка
// public class ChargePointConfiguration : IEntityTypeConfiguration<ChargePointEntity>
// {
//     public void Configure(EntityTypeBuilder<ChargePointEntity> builder)
//     {
//         builder.Property(c => c.ChargePointId)
//             .HasMaxLength(100)
//             .IsRequired();
//
//         builder.Property(c => c.Status)
//             .HasMaxLength(50)
//             .IsRequired();
//
//         builder.HasOne(c => c.Station)
//             .WithMany(s => s.ChargePoints)
//             .HasForeignKey(c => c.StationId);
//     }
// }
//
// // Коннектор
// public class ConnectorConfiguration : IEntityTypeConfiguration<ConnectorEntity>
// {
//     public void Configure(EntityTypeBuilder<ConnectorEntity> builder)
//     {
//         builder.Property(c => c.ConnectorId)
//             .IsRequired();
//
//         builder.Property(c => c.Status)
//             .HasMaxLength(50);
//
//         builder.HasOne(c => c.ChargePoint)
//             .WithMany(cp => cp.Connectors)
//             .HasForeignKey(c => c.ChargePointId);
//     }
// }
//
// // Пользователи
// public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
// {
//     public void Configure(EntityTypeBuilder<UserEntity> builder)
//     {
//         builder.Property(u => u.FullName)
//             .HasMaxLength(200)
//             .IsRequired();
//
//         builder.Property(u => u.Email)
//             .HasMaxLength(150);
//
//         builder.Property(u => u.Phone)
//             .HasMaxLength(50);
//
//         builder.HasOne(u => u.Role)
//             .WithMany()
//             .HasForeignKey(u => u.RoleId);
//     }
// }
//
// // Роли
// public class RoleConfiguration : IEntityTypeConfiguration<RoleEntity>
// {
//     public void Configure(EntityTypeBuilder<RoleEntity> builder)
//     {
//         builder.Property(r => r.Name)
//             .HasMaxLength(100)
//             .IsRequired();
//     }
// }
//
// // Разрешения
// public class PermissionConfiguration : IEntityTypeConfiguration<PermissionEntity>
// {
//     public void Configure(EntityTypeBuilder<PermissionEntity> builder)
//     {
//         builder.Property(p => p.Code)
//             .HasMaxLength(100)
//             .IsRequired();
//
//         builder.Property(p => p.Description)
//             .HasMaxLength(300);
//     }
// }
//
// // Привязка пользователей к станции
// public class StationUsersConfiguration : IEntityTypeConfiguration<StationUsersEntity>
// {
//     public void Configure(EntityTypeBuilder<StationUsersEntity> builder)
//     {
//         builder.HasOne(su => su.Station)
//             .WithMany(s => s.StationUsers)
//             .HasForeignKey(su => su.StationId);
//
//         builder.HasOne(su => su.User)
//             .WithMany()
//             .HasForeignKey(su => su.UserId);
//     }
// }
//
// // История зарядок
// public class HistoryChargesConfiguration : IEntityTypeConfiguration<HistoryChargesEntity>
// {
//     public void Configure(EntityTypeBuilder<HistoryChargesEntity> builder)
//     {
//         builder.Property(h => h.Status)
//             .HasMaxLength(50)
//             .IsRequired();
//
//         builder.HasOne(h => h.Station)
//             .WithMany()
//             .HasForeignKey(h => h.StationId);
//     }
// }
//
// // Динамические тарифы
// public class DynamicTrafficPriceConfiguration : IEntityTypeConfiguration<DynamicTrafficPriceEntity>
// {
//     public void Configure(EntityTypeBuilder<DynamicTrafficPriceEntity> builder)
//     {
//         builder.Property(d => d.PricePerKWh)
//             .HasColumnType("decimal(18,2)");
//
//         builder.HasOne(d => d.Station)
//             .WithMany()
//             .HasForeignKey(d => d.StationId);
//     }
// }