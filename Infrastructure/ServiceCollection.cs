using Application;
using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceCollection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication(configuration);
        
        string postgresConnectionString = configuration.GetConnectionString("Postgres");
            
        // if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        // {
        //     services.AddDbContext<OCPPCoreContext>(options => 
        //         options.UseNpgsql(postgresConnectionString, 
        //             b => b.MigrationsAssembly(typeof(OCPPCoreContext).Assembly.FullName)), ServiceLifetime.Transient);
        // }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(postgresConnectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>()!);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
