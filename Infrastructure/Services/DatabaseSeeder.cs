using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities.Station;
using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class DatabaseSeeder
    {
        private readonly IApplicationDbContext _context;

        public DatabaseSeeder(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
            await SeedPermissionsAsync();
            await SeedDefaultUsersAsync();
            await _context.SaveChangesAsync();
        }

        private async Task SeedRolesAsync()
        {
            if (!await _context.Roles.AnyAsync())
            {
                var roles = new[]
                {
                    RoleEntity.SuperAdmin,
                    RoleEntity.CompanyOwner,
                    RoleEntity.CompanyEmployee
                };

                foreach (var role in roles)
                {
                    // Сбрасываем ID для автоинкремента
                    role.Id = 0;
                    _context.Roles.Add(role);
                }
            }
        }

        private async Task SeedPermissionsAsync()
        {
            if (!await _context.Permissions.AnyAsync())
            {
                var permissions = new[]
                {
                    // Admin permissions
                    new PermissionEntity { Name = "Admin.View", Resource = "admin", Action = "view", Description = "Просмотр админ панели" },
                    new PermissionEntity { Name = "Admin.Manage", Resource = "admin", Action = "manage", Description = "Управление админ панелью" },

                    // Company permissions
                    new PermissionEntity { Name = "Company.View", Resource = "company", Action = "view", Description = "Просмотр данных компании" },
                    new PermissionEntity { Name = "Company.Manage", Resource = "company", Action = "manage", Description = "Управление компанией" },

                    // Charge points permissions
                    new PermissionEntity { Name = "ChargePoints.View", Resource = "chargepoints", Action = "view", Description = "Просмотр зарядных станций" },
                    new PermissionEntity { Name = "ChargePoints.Manage", Resource = "chargepoints", Action = "manage", Description = "Управление зарядными станциями" },
                    new PermissionEntity { Name = "ChargePoints.Control", Resource = "chargepoints", Action = "control", Description = "Управление зарядкой" },

                    // Transactions permissions
                    new PermissionEntity { Name = "Transactions.View", Resource = "transactions", Action = "view", Description = "Просмотр транзакций" },

                    // Users permissions
                    new PermissionEntity { Name = "Users.View", Resource = "users", Action = "view", Description = "Просмотр пользователей" },
                    new PermissionEntity { Name = "Users.Manage", Resource = "users", Action = "manage", Description = "Управление пользователями" }
                };

                foreach (var permission in permissions)
                {
                    _context.Permissions.Add(permission);
                }

                await _context.SaveChangesAsync();

                // Назначаем права ролям
                await AssignPermissionsToRolesAsync();
            }
        }

        private async Task AssignPermissionsToRolesAsync()
        {
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            var companyOwnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "CompanyOwner");
            var companyEmployeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "CompanyEmployee");

            if (superAdminRole != null)
            {
                var allPermissions = await _context.Permissions.ToListAsync();
                foreach (var permission in allPermissions)
                {
                    _context.RolePermissions.Add(new RolePermissionEntity
                    {
                        RoleId = superAdminRole.Id,
                        PermissionId = permission.Id
                    });
                }
            }

            if (companyOwnerRole != null)
            {
                var companyPermissions = await _context.Permissions
                    .Where(p => p.Resource == "company" || p.Resource == "chargepoints" || p.Resource == "transactions" || p.Resource == "users")
                    .ToListAsync();

                foreach (var permission in companyPermissions)
                {
                    _context.RolePermissions.Add(new RolePermissionEntity
                    {
                        RoleId = companyOwnerRole.Id,
                        PermissionId = permission.Id
                    });
                }
            }

            if (companyEmployeeRole != null)
            {
                var employeePermissions = await _context.Permissions
                    .Where(p => p.Resource == "chargepoints" && p.Action == "view" ||
                               p.Resource == "transactions" && p.Action == "view")
                    .ToListAsync();

                foreach (var permission in employeePermissions)
                {
                    _context.RolePermissions.Add(new RolePermissionEntity
                    {
                        RoleId = companyEmployeeRole.Id,
                        PermissionId = permission.Id
                    });
                }
            }
        }

        private async Task SeedDefaultUsersAsync()
        {
            if (!await _context.Users.AnyAsync())
            {
                // Создаем тестовую компанию
                var testCompany = new CompanyEntity
                {
                    Name = "Test Charging Company",
                    Description = "Тестовая компания для зарядных станций",
                    ContactEmail = "admin@testcompany.com",
                    ContactPhone = "+7 (999) 123-45-67",
                    Address = "г. Москва, ул. Тестовая, д. 1",
                    Balance = 10000,
                    IsActive = true
                };

                _context.Companies.Add(testCompany);
                await _context.SaveChangesAsync();

                // Создаем тестовую станцию
                var testStation = new StationEntity
                {
                    Name = "Тестовая зарядная станция",
                    Description = "Станция для тестирования OCPP протокола",
                    Address = "г. Москва, ул. Ленина, д. 10",
                    Latitude = 55.7558m,
                    Longitude = 37.6176m,
                    City = "Москва",
                    Region = "Москва",
                    PostalCode = "101000",
                    Status = "Active",
                    CompanyId = testCompany.Id
                };

                _context.Stations.Add(testStation);
                await _context.SaveChangesAsync();

                // Создаем тестовый ChargePoint
                var testChargePoint = new ChargePointEntity
                {
                    ChargePointId = "CP001", // Этот ID используется для OCPP подключения: ws://localhost:5088/ocpp/CP001
                    Name = "Тестовый зарядный пункт",
                    ProtocolVersion = "1.6",
                    Vendor = "Test Vendor",
                    Model = "Test Model",
                    SerialNumber = "SN001",
                    FirmwareVersion = "1.0.0",
                    MeterType = "Electricity",
                    MeterSerialNumber = "MSN001",
                    StationId = testStation.Id
                };

                _context.ChargePoints.Add(testChargePoint);
                await _context.SaveChangesAsync();

                // Создаем суперадмина
                var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
                if (superAdminRole != null)
                {
                    var superAdmin = new UserEntity
                    {
                        Email = "admin@echarger.com",
                        FirstName = "Super",
                        LastName = "Admin",
                        PasswordHash = "AQAAAAEAACcQAAAAEBLjouNqAeB4Jh1ZyK8H4Y3CwQqRO8n3HbQXv5zOjzPJ6kYH8jQ7vO4Bq4Eoq5w==", // Password: Admin123!
                        PasswordSalt = "test-salt",
                        IsActive = true,
                        IsEmailConfirmed = true
                    };

                    _context.Users.Add(superAdmin);
                    await _context.SaveChangesAsync();

                    _context.UserRoles.Add(new UserRoleEntity
                    {
                        UserId = superAdmin.Id,
                        RoleId = superAdminRole.Id
                    });
                }

                // Создаем владельца компании
                var companyOwnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "CompanyOwner");
                if (companyOwnerRole != null)
                {
                    var companyOwner = new UserEntity
                    {
                        Email = "owner@testcompany.com",
                        FirstName = "John",
                        LastName = "Doe",
                        Phone = "+7 (999) 987-65-43",
                        PasswordHash = "AQAAAAEAACcQAAAAEBLjouNqAeB4Jh1ZyK8H4Y3CwQqRO8n3HbQXv5zOjzPJ6kYH8jQ7vO4Bq4Eoq5w==", // Password: Admin123!
                        PasswordSalt = "test-salt",
                        CompanyId = testCompany.Id,
                        IsActive = true,
                        IsEmailConfirmed = true
                    };

                    _context.Users.Add(companyOwner);
                    await _context.SaveChangesAsync();

                    _context.UserRoles.Add(new UserRoleEntity
                    {
                        UserId = companyOwner.Id,
                        RoleId = companyOwnerRole.Id
                    });
                }
            }
        }
    }
}
